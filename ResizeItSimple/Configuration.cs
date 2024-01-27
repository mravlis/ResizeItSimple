using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Xml;

namespace ResizeItSimple
{

	/// <summary>
	/// Represents configuration properties and atributes.
	/// </summary>
	internal sealed class Configuration
	{

		/// <summary>
		/// Static managed type of the Encoder class, for efficient dynamic type operations.
		/// </summary>
		private static readonly Type EncoderType = typeof(Encoder);

		/// <summary>
		/// Static dictionary of known encoders, organized by encoder MIME type.
		/// </summary>
		private static readonly Dictionary<string, ImageCodecInfo> EncoderDictionary = new Dictionary<string, ImageCodecInfo>();

		/// <summary>
		/// Static list of supported graphics file extensions.
		/// </summary>
		private static readonly List<string> SupportedFileExtensions = new List<string>();

		/// <summary>
		/// Boolean indicating whether to keep the source aspect ratio when resizing.
		/// </summary>
		private readonly bool _keepAspectRatio = true;

		/// <summary>
		/// The resize ratio.
		/// </summary>
		private readonly float _ratio = 0.1F;

		/// <summary>
		/// The fixed output width.
		/// </summary>
		private readonly int _fixedWidth = 1024;

		/// <summary>
		/// The fixed output height.
		/// </summary>
		private readonly int _fixedHeight = 768;

		/// <summary>
		/// The output folder name.
		/// </summary>
		private readonly string _outputFolderName = @"Resized";

		/// <summary>
		/// The output file name pattern.
		/// </summary>
		private readonly string _outputFileSuffix = @"{0} (Resized)";

		/// <summary>
		/// Boolean indicating whether the output should be skipped if output file already exists.
		/// </summary>
		private readonly bool _skipIfOutputAlreadyExists = false;

		/// <summary>
		/// Boolean indicating whether to copy metadata from source image.
		/// </summary>
		private readonly bool _copyMetadata = true;

		/// <summary>
		/// Number of maximum number of threads to be used.
		/// </summary>
		private readonly int _maxDegreeOfParallelism = -1;

		/// <summary>
		/// Boolean indicating whether recursive directory search is enabled.
		/// </summary>
		private readonly bool _recursiveDirectorySearch = false;

		/// <summary>
		/// The image codec used to save resized files.
		/// </summary>
		private readonly ImageCodecInfo _imageCodecInfo = null;

		/// <summary>
		/// The output extension, defined by the chosen codec.
		/// </summary>
		private readonly string _outputExtension = string.Empty;

		/// <summary>
		/// The encoder parameters to be used when a specific coded is specified.
		/// </summary>
		private readonly EncoderParameters _encoderParameters = null;

		/// <summary>
		/// Compositing quality to be used while resizing.
		/// </summary>
		private readonly CompositingQuality _compositingQuality = CompositingQuality.HighQuality;

		/// <summary>
		/// Interpolation mode to be used while resizing.
		/// </summary>
		private readonly InterpolationMode _interpolationMode = InterpolationMode.HighQualityBicubic;

		/// <summary>
		/// Pixel offset mode to be used while resizing.
		/// </summary>
		private readonly PixelOffsetMode _pixelOffsetMode = PixelOffsetMode.HighQuality;

		/// <summary>
		/// Smoothing mode to be used while resizing.
		/// </summary>
		private readonly SmoothingMode _smoothingMode = SmoothingMode.HighQuality;

		/// <summary>
		/// Static constructor (AKA type initializer).
		/// </summary>
		static Configuration()
		{

			// Get list of all known encoders
			ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();

			// Enlist mime types and file extensions
			foreach (ImageCodecInfo codecInfo in encoders)
			{

				// Add encoder to dictionary
				EncoderDictionary.Add(codecInfo.MimeType, codecInfo);

				// Add supported file extensions
				SupportedFileExtensions.AddRange(codecInfo.FilenameExtension.ToLower().Replace(@"*.", string.Empty).Split(';'));

			}

		}

		/// <summary>
		/// Creates a new instance of the Configuration class using default configuration parameters.
		/// </summary>
		public Configuration()
		{
		}

		/// <summary>
		/// Creates a new instance of the Configuration class.
		/// </summary>
		/// <param name="configurationFileName">The configuration file name.</param>
		public Configuration(string configurationFileName) : this()
		{

			XmlDocument configDocument;

			// File does not exist. Continue with default parameters.
			if (!System.IO.File.Exists(configurationFileName))
				return;

			try
			{

				// Creates new empty in-memory XML document to hold configuration information
				configDocument = new XmlDocument();

				// Loads the configuration file data
				configDocument.Load(configurationFileName);

			}
			catch
			{

				// Configuration file is not valid. Continue with default values.
				return;

			}

			// Get CopyMetadata configuration node
			XmlNode configNode = configDocument.SelectSingleNode(@"/Configuration/CopyMetadata");

			try
			{

				// Check if node exists and if it has been assigned value
				if ((configNode != null) && !string.IsNullOrWhiteSpace(configNode.InnerText))
					_copyMetadata = bool.Parse(configNode.InnerText);

			}
			catch { }

			// Get Resize configuration node
			configNode = configDocument.SelectSingleNode(@"/Configuration/Resize");

			try
			{

				// Check if node exists
				if (configNode != null)
				{

					// Get KeepAspectRatio attribute
					XmlAttribute attribute = configNode.Attributes["KeepAspectRatio"];

					// Check if atribute exists and if it has been assigned value
					if ((attribute != null) && !string.IsNullOrWhiteSpace(attribute.Value))
						_keepAspectRatio = bool.Parse(attribute.Value);

					// Get Ratio attribute
					attribute = configNode.Attributes["Ratio"];

					// Check if atribute exists and if it has been assigned value
					if ((attribute != null) && !string.IsNullOrWhiteSpace(attribute.Value))
						_ratio = float.Parse(attribute.Value);

					// Get FixedWidth attribute
					attribute = configNode.Attributes["FixedWidth"];

					// Check if atribute exists and if it has been assigned value
					if ((attribute != null) && !string.IsNullOrWhiteSpace(attribute.Value))
						_fixedWidth = int.Parse(attribute.Value);

					// Get FixedHeight attribute
					attribute = configNode.Attributes["FixedHeight"];

					// Check if atribute exists and if it has been assigned value
					if ((attribute != null) && !string.IsNullOrWhiteSpace(attribute.Value))
						_fixedHeight = int.Parse(attribute.Value);

				}

			}
			catch { }

			// Get OutputFolderName configuration node
			configNode = configDocument.SelectSingleNode(@"/Configuration/OutputFolderName");

			try
			{

				// Check if node exists and if it has been assigned value
				if ((configNode != null) && !string.IsNullOrWhiteSpace(configNode.InnerText))
					_outputFolderName = configNode.InnerText;

			}
			catch { }

			// Get OutputFileSuffix configuration node
			configNode = configDocument.SelectSingleNode(@"/Configuration/OutputFileSuffix");

			try
			{

				// Check if node exists and if it has been assigned value
				if ((configNode != null) && !string.IsNullOrWhiteSpace(configNode.InnerText))
					_outputFileSuffix = configNode.InnerText;

			}
			catch { }

			// Get SkipIfOutputAlreadyExists configuration node
			configNode = configDocument.SelectSingleNode(@"/Configuration/SkipIfOutputAlreadyExists");

			try
			{

				// Check if node exists and if it has been assigned value
				if ((configNode != null) && !string.IsNullOrWhiteSpace(configNode.InnerText))
					_skipIfOutputAlreadyExists = bool.Parse(configNode.InnerText);

			}
			catch { }

			// Get MaxDegreeOfParallelism configuration node
			configNode = configDocument.SelectSingleNode(@"/Configuration/MaxDegreeOfParallelism");

			try
			{

				// Check if node exists and if it has been assigned value
				if ((configNode != null) && !string.IsNullOrWhiteSpace(configNode.InnerText))
					_maxDegreeOfParallelism = int.Parse(configNode.InnerText);

			}
			catch { }

			// Get RecursiveDirectorySearch configuration node
			configNode = configDocument.SelectSingleNode(@"/Configuration/RecursiveDirectorySearch");

			try
			{

				// Check if node exists and if it has been assigned value
				if ((configNode != null) && !string.IsNullOrWhiteSpace(configNode.InnerText))
					_recursiveDirectorySearch = bool.Parse(configNode.InnerText);

			}
			catch { }

			// Get ImageCodec configuration node
			configNode = configDocument.SelectSingleNode(@"/Configuration/ImageCodec");

			try
			{

				// Check if ImageCodec node was found
				if (configNode != null)
				{

					// Get MimeType attribute
					XmlAttribute mimeTypeAttribute = configNode.Attributes["MimeType"];

					// Check if atribute exists and if it has been assigned value
					if ((mimeTypeAttribute != null) && !string.IsNullOrWhiteSpace(mimeTypeAttribute.Value))
					{

						// If key is not found, an exception is thrown and this property is not configured
						_imageCodecInfo = EncoderDictionary[mimeTypeAttribute.Value];

						// Get first extension to be used by the output
						_outputExtension = _imageCodecInfo.FilenameExtension.Replace("*", string.Empty).ToLower().Split(';')[0];

					}
						
					// Temporary list of encoder parameters
					List<EncoderParameter> parameterList = new List<EncoderParameter>();

					// Check all child XML nodes for parameters
					foreach (XmlNode parameterNode in configNode.ChildNodes)
					{

						// Get encoder parameter from this XML node (can return nulls)
						EncoderParameter parameter = GetEncoderParameter(parameterNode);

						// Don't add parameter if it is null
						if (parameter != null)
							parameterList.Add(parameter);

					}

					// Get final parameter count
					int totalParametersFound = parameterList.Count;

					// Create new parameter collection
					_encoderParameters = new EncoderParameters(totalParametersFound);

					// Get encoder parameter for each child parameter node
					for (int parameterIndex = 0; parameterIndex < totalParametersFound; parameterIndex++)
						_encoderParameters.Param[parameterIndex] = parameterList[parameterIndex];

				}

			}
			catch { }

			// Get CompositingQuality configuration node
			configNode = configDocument.SelectSingleNode(@"/Configuration/CompositingQuality");

			try
			{

				// Check if node exists and if it has been assigned value
				if ((configNode != null) && !string.IsNullOrWhiteSpace(configNode.InnerText))
					_compositingQuality = (CompositingQuality)Enum.Parse(typeof(CompositingQuality), configNode.InnerText);

			}
			catch { }

			// Get InterpolationMode configuration node
			configNode = configDocument.SelectSingleNode(@"/Configuration/InterpolationMode");

			try
			{

				// Check if node exists and if it has been assigned value
				if ((configNode != null) && !string.IsNullOrWhiteSpace(configNode.InnerText))
					_interpolationMode = (InterpolationMode)Enum.Parse(typeof(InterpolationMode), configNode.InnerText);

			}
			catch { }

			// Get PixelOffsetMode configuration node
			configNode = configDocument.SelectSingleNode(@"/Configuration/PixelOffsetMode");

			try
			{

				// Check if node exists and if it has been assigned value
				if ((configNode != null) && !string.IsNullOrWhiteSpace(configNode.InnerText))
					_pixelOffsetMode = (PixelOffsetMode)Enum.Parse(typeof(PixelOffsetMode), configNode.InnerText);

			}
			catch { }

			// Get SmoothingMode configuration node
			configNode = configDocument.SelectSingleNode(@"/Configuration/SmoothingMode");

			try
			{

				// Check if node exists and if it has been assigned value
				if ((configNode != null) && !string.IsNullOrWhiteSpace(configNode.InnerText))
					_smoothingMode = (SmoothingMode)Enum.Parse(typeof(SmoothingMode), configNode.InnerText);

			}
			catch { }

		}

		/// <summary>
		/// Retrieves an encoder parameter from a XML encoder parameter node.
		/// </summary>
		/// <param name="parameterNode">The XML node representing the encoder.</param>
		/// <returns>A EncoderParameter object, or null if the XML node is null or has invalid parameters.</returns>
		private static EncoderParameter GetEncoderParameter(XmlNode parameterNode)
		{

			// Check if the node is different than null
			if (parameterNode == null)
				return null;

			// Get parameter (node) value
			string parameterValue = parameterNode.InnerText;

			// If parameter has no value then skip it
			if (string.IsNullOrWhiteSpace(parameterValue))
				return null;

			// Get paramter name from the XML node name - it is known that this can't be null or empty
			string parameterName = parameterNode.Name;

			// Get field information from type
			FieldInfo fieldInfo = EncoderType.GetField(parameterName, BindingFlags.Static | BindingFlags.Public);

			// The encoder name was not recognized
			if (fieldInfo == null)
				return null;

			// Gets the value for the static field using null reference
			Encoder referencedEncoder = (Encoder)fieldInfo.GetValue(null);

			// TODO: Read all different value types
			long value = long.Parse(parameterValue);

			// Return parameter for the specified value
			return new EncoderParameter(referencedEncoder, value);

		}

		/// <summary>
		/// Gets an array of supported file extensions.
		/// </summary>
		public string[] SupportedExtensions => SupportedFileExtensions.ToArray();

		/// <summary>
		/// Gets a boolean indicating whether to keep the source aspect ratio.
		/// </summary>
		public bool KeepAspectRatio => _keepAspectRatio;

		/// <summary>
		/// Gets a float indicating the target resizing ratio.
		/// </summary>
		public float Ratio => _ratio;

		/// <summary>
		/// Gets the target output width.
		/// </summary>
		public int FixedWidth => _fixedWidth;

		/// <summary>
		/// Gets the target output height.
		/// </summary>
		public int FixedHeight => _fixedHeight;

		/// <summary>
		/// Gets the output folder name.
		/// </summary>
		public string OutputFolderName => _outputFolderName;

		/// <summary>
		/// Gets the output file name suffix.
		/// </summary>
		public string OutputFileSuffix => _outputFileSuffix;

		/// <summary>
		/// Gets a boolean indicating whether the output should be skipped if the output file already exists.
		/// </summary>
		/// <remarks>Setting this to false will instruct the program to overwrite an existing file.</remarks>
		public bool SkipIfOutputAlreadyExists => _skipIfOutputAlreadyExists;

		/// <summary>
		/// Gets a boolean indicating whether to copy metadata from source.
		/// </summary>
		public bool CopyMetadata => _copyMetadata;

		/// <summary>
		/// Gets the maximum degree of parallelism to be used on parallel conversions.
		/// </summary>
		public int MaxDegreeOfParallelism => _maxDegreeOfParallelism;

		/// <summary>
		/// Gets a boolean indicating whether recursive directory search is enabled.
		/// </summary>
		public bool RecursiveDirectorySearch => _recursiveDirectorySearch;

		/// <summary>
		/// Gets the configured image codec.
		/// </summary>
		public ImageCodecInfo ImageCodec => _imageCodecInfo;

		/// <summary>
		/// Gets the output extension defined by the codec.
		/// </summary>
		/// <remarks>If no codec is configured, this property returns an empty string.</remarks>
		public string OutputExtension => _outputExtension;

		/// <summary>
		/// Gets the configured encoder parameters.
		/// </summary>
		public EncoderParameters EncoderParameters => _encoderParameters;

		/// <summary>
		/// Gets the configured compositing quality to be used while resizing.
		/// </summary>
		public CompositingQuality CompositingQuality => _compositingQuality;

		/// <summary>
		/// Gets the configured interpolation mode to be used while resizing.
		/// </summary>
		public InterpolationMode InterpolationMode => _interpolationMode;

		/// <summary>
		/// Gets the configured pixel offset mode to be used while resizing.
		/// </summary>
		public PixelOffsetMode PixelOffsetMode => _pixelOffsetMode;

		/// <summary>
		/// Gets the configured smoothing mode to be used while resizing.
		/// </summary>
		public SmoothingMode SmoothingMode => _smoothingMode;

	}

}
