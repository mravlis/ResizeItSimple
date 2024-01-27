using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace ResizeItSimple
{

	/// <summary>
	/// Main program class.
	/// </summary>
	class Program
	{

		/// <summary>
		/// The configuration file name.
		/// </summary>
		private const string ConfigurationFileName = @"Configuration.xml";

		/// <summary>
		/// The configuration object.
		/// </summary>
		private static Configuration _configuration;

		/// <summary>
		/// Supported graphics file extensions.
		/// </summary>
		private static List<string> _supportedExtensions;

		/// <summary>
		/// Entry point.
		/// </summary>
		/// <param name="args">The list of arguments.</param>
		/// <returns>0 if execution was successfull, a value different than zero if an error occurs.</returns>
		static int Main(string[] args)
		{

			try
			{

				// Checks argument array reference
				if (args == null)
					return 0;

				// Reads configuration from the default configuration file name.
				_configuration = new Configuration(ConfigurationFileName);

				// Gets the configured, supported file extensions.
				_supportedExtensions = new List<string>(_configuration.SupportedExtensions);

				// Create new list of files to be processed
				List<FileInfo> fileList = new List<FileInfo>();

				// Process each argument (can be a file or a directory)
				foreach (string argument in args)
				{
					
					// Check if argument is a file or directory
					if (File.Exists(argument))
					{
						fileList.Add(new FileInfo(argument));
					}
					else if (Directory.Exists(argument))
					{

						// Creates new directory info from the specified argument
						DirectoryInfo dirInfo = new DirectoryInfo(argument);

						// Defines whether to perform recursive search or not
						SearchOption searchOption = _configuration.RecursiveDirectorySearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

						// Get directory files
						fileList.AddRange(dirInfo.GetFiles("*.*", searchOption));

					}

				}

				// Set parallel processing options
				ParallelOptions parallelOptions = new ParallelOptions
				{
					MaxDegreeOfParallelism = _configuration.MaxDegreeOfParallelism
				};

				// Run conversions in parallel
				var processingResults = Parallel.ForEach(fileList, parallelOptions, ResizeImage);						

				// Return zero to indicate a successfull run.
				return 0;

			}
			catch
			{

				// We got an error, return negative 0xFFFFFF to diferentiate from normal execution.
				return -1;

			}

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileInfo"></param>
		/// <returns></returns>
		private static string GetTargetPath(FileInfo fileInfo)
		{

			// Parameter check
			if (fileInfo == null)
				throw new ArgumentNullException(nameof(fileInfo));

			string targetDirectory;

			// Computes the target directory name
			if (string.IsNullOrWhiteSpace(_configuration.OutputFolderName))
				targetDirectory = fileInfo.Directory.FullName;
			else
				targetDirectory = Path.Combine(fileInfo.Directory.FullName, _configuration.OutputFolderName);

			// Check if the target directory exists
			if (!Directory.Exists(targetDirectory))
				Directory.CreateDirectory(targetDirectory);

			// Get just the file name
			string justFileName = Path.GetFileNameWithoutExtension(fileInfo.FullName);

			string newExtension;

			// Choose output extension
			if (_configuration.ImageCodec == null)
				newExtension = fileInfo.Extension;
			else
				newExtension = _configuration.OutputExtension;

			// Builds full file name
			return Path.Combine(targetDirectory, $"{justFileName}{_configuration.OutputFileSuffix}{newExtension}");

		}

		/// <summary>
		/// Checks if the file extension is a supported graphics file.
		/// </summary>
		/// <param name="fileInfo">A FileInfo object.</param>
		/// <returns>True if the file extension is supported, false otherwise.</returns>
		private static bool IsFileSupported(FileInfo fileInfo)
		{

			// Parameter check
			if (fileInfo == null)
				throw new ArgumentNullException(nameof(fileInfo));

			// Get file extension without leading period
			string fileExtension = fileInfo.Extension.Remove(0, 1).ToLower();

			// Check if file extension is supported
			return _supportedExtensions.Contains(fileExtension);			

		}

		/// <summary>
		/// Resizes an image, saving the new resized image.
		/// </summary>
		/// <param name="sourceImageFile">The source image FileInfo object.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="sourceImageFile"/> is null.</exception>
		/// <exception cref="FileNotFoundException">Thrown if <paramref name="sourceImageFile"/> is not found.</exception>
		private static void ResizeImage(FileInfo sourceImageFile)
		{

			// Parameter check
			if (sourceImageFile == null)
				throw new ArgumentNullException(nameof(sourceImageFile));

			// Get full path of the source image
			string sourceImagePath = sourceImageFile.FullName;

			// Check if source image file exists
			if (!sourceImageFile.Exists)
				throw new FileNotFoundException(@"Could not find the specified file.", sourceImagePath);

			Bitmap sourceImage = null;
			Bitmap resizedImage = null;
			Graphics graphics = null;

			try
			{

				// Gets target file path
				string resizedImagePath = GetTargetPath(sourceImageFile);

				// Check if we should skip files that already exist
				if (_configuration.SkipIfOutputAlreadyExists && File.Exists(resizedImagePath))
					return;

				// If source file is not supported, do nothing.
				if (!IsFileSupported(sourceImageFile))
					return;

				// Opens the source image
				sourceImage = new Bitmap(sourceImagePath);

				int targetWidth;
				int targetHeight;

				// Confirm target image size
				if (_configuration.KeepAspectRatio)
				{
					targetHeight = (int)(sourceImage.Height * _configuration.Ratio);
					targetWidth = (int)(sourceImage.Width * _configuration.Ratio);
				}
				else
				{
					targetHeight = _configuration.FixedHeight;
					targetWidth = _configuration.FixedWidth;
				}

				// Creates a new empty image as bitmap
				resizedImage = new Bitmap(targetWidth, targetHeight);

				// Creates a new graphics writer for the new bitmap
				graphics = Graphics.FromImage(resizedImage);

				// Set new image compositing quality
				graphics.CompositingQuality = _configuration.CompositingQuality;

				// Set new image interpolation mode
				graphics.InterpolationMode = _configuration.InterpolationMode;

				// Set new image pixel offset mode
				graphics.PixelOffsetMode = _configuration.PixelOffsetMode;

				// Set new image smoothing mode
				graphics.SmoothingMode = _configuration.SmoothingMode;

				// Draw image on target, resized image.
				graphics.DrawImage(sourceImage, 0, 0, targetWidth, targetHeight);

				// Copy source image properties (metadata)
				if (_configuration.CopyMetadata)
					foreach (var propertyItem in sourceImage.PropertyItems)
						resizedImage.SetPropertyItem(propertyItem);

				// Check if a target image codec was specified
				if (_configuration.ImageCodec == null)
					resizedImage.Save(resizedImagePath, sourceImage.RawFormat);
				else
					resizedImage.Save(resizedImagePath, _configuration.ImageCodec, _configuration.EncoderParameters);

			}
			finally
			{

				if (graphics != null)
					graphics.Dispose();

				if (resizedImage != null)
					resizedImage.Dispose();

				if (sourceImage != null)
					sourceImage.Dispose();

			}

		}

	}

}