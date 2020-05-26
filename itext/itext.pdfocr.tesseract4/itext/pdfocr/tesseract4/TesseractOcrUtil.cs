using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Common.Logging;
using iText.IO.Util;
using Tesseract;

namespace iText.Pdfocr.Tesseract4 {
    /// <summary>
    /// Utilities class to work with tesseract command line tool and image
    /// preprocessing using
    /// <see cref="Net.Sourceforge.Lept4j.ILeptonica"/>.
    /// </summary>
    /// <remarks>
    /// Utilities class to work with tesseract command line tool and image
    /// preprocessing using
    /// <see cref="Net.Sourceforge.Lept4j.ILeptonica"/>.
    /// These all methods have to be ported to .Net manually.
    /// </remarks>
    internal sealed class TesseractOcrUtil {
        /// <summary>The logger.</summary>
        internal static readonly ILog LOGGER = LogManager.GetLogger(typeof(iText.Pdfocr.Tesseract4.TesseractOcrUtil
            ));

        /// <summary>List of pages of the image that is being processed.</summary>
        private IList<Pix> imagePages = new List<Pix>();

        /// <summary>
        /// Creates a new
        /// <see cref="TesseractOcrUtil"/>
        /// instance.
        /// </summary>
        internal TesseractOcrUtil() {
        }

        /// <summary>Runs given command.</summary>
        /// <param name="command">
        /// 
        /// <see cref="System.Collections.IList{E}"/>
        /// of command line arguments
        /// </param>
        /// <param name="isWindows">true is current os is windows</param>
        internal static void RunCommand(IList<String> command, bool isWindows) {
            Process process = null;
            try
            {
                if (isWindows)
                {
                    String cmd = "";
                    for (int i = 1; i < command.Count; ++i)
                    {
                        cmd += command[i] + " ";
                    }

                    process = new Process();
                    process.StartInfo.FileName = command[0];
                    process.StartInfo.Arguments = cmd;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.Start();

                    //* Read the output (or the error)
                    string output = process.StandardOutput.ReadToEnd();
                    LogManager.GetLogger(typeof(iText.Pdfocr.Tesseract4.TesseractOcrUtil)).Info(output);
                    string err = process.StandardError.ReadToEnd();
                    LogManager.GetLogger(typeof(iText.Pdfocr.Tesseract4.TesseractOcrUtil)).Info(err);
                }
                else
                {
                    String cmd_str = "bash" + "-c" + String.Join(" ", command);
                    ProcessStartInfo pb = new ProcessStartInfo(cmd_str);
                    //NOSONAR
                    process = System.Diagnostics.Process.Start(pb);
                }
                bool cmdSucceeded = process.WaitForExit(3 * 60 * 60 * 1000);
                if (!cmdSucceeded)
                {
                    throw new Tesseract4OcrException(Tesseract4OcrException.TesseractFailed)
                        .SetMessageParams(String.Join(" ", command));
                }
            }
            catch (Exception e)
            {
                LogManager.GetLogger(typeof(iText.Pdfocr.Tesseract4.TesseractOcrUtil))
                    .Error(MessageFormatUtil.Format(Tesseract4LogMessageConstant.TesseractFailed,
                                e.Message));
                throw new Tesseract4OcrException(Tesseract4OcrException.TesseractFailed)
                    .SetMessageParams(e.Message);
            }
        }

        /// <summary>Reads required page from provided tiff image.</summary>
        /// <param name="inputFile">
        /// input image as
        /// <see cref="System.IO.FileInfo"/>
        /// </param>
        /// <param name="pageNumber">number of page</param>
        /// <returns>
        /// result
        /// <see cref="Tesseract.Pix"/>
        /// object created from
        /// given image
        /// </returns>
        internal static Pix ReadPixPageFromTiff(FileInfo inputFile, int pageNumber) {
            // read image
            PixArray pixa = PixArray.LoadMultiPageTiffFromFile(inputFile.FullName);
            int size = pixa.Count;
            // in case page number is incorrect
            if (pageNumber >= size)
            {
                LogManager.GetLogger(typeof(iText.Pdfocr.Tesseract4.TesseractOcrUtil))
                    .Info(MessageFormatUtil.Format(
                        Tesseract4LogMessageConstant.PageNumberIsIncorrect,
                        pageNumber,
                        inputFile.FullName));
                return null;
            }
            Pix pix = pixa.GetPix(pageNumber);
            DestroyPixa(pixa);
            // return required page to be preprocessed
            return pix;
        }

        /// <summary>
        /// Performs default image preprocessing and saves result to a temporary
        /// file.
        /// </summary>
        /// <param name="pix">
        /// 
        /// <see cref="Tesseract.Pix"/>
        /// object to be processed
        /// </param>
        /// <returns>
        /// path to a created preprocessed image file
        /// as
        /// <see cref="System.String"/>
        /// </returns>
        internal static String PreprocessPixAndSave(Pix pix) {
            // preprocess image
            pix = PreprocessPix(pix);
            // save preprocessed file
            String tmpFileName = GetTempDir() + System.Guid.NewGuid().ToString() + ".png";
            pix.Save(tmpFileName, ImageFormat.Png);
            DestroyPix(pix);
            return tmpFileName;
        }

        /// <summary>Performs default image preprocessing.</summary>
        /// <remarks>
        /// Performs default image preprocessing.
        /// It includes the following actions:
        /// removing alpha channel,
        /// converting to grayscale,
        /// thresholding.
        /// </remarks>
        /// <param name="pix">
        /// 
        /// <see cref="Tesseract.Pix"/>
        /// object to be processed
        /// </param>
        /// <returns>
        /// preprocessed
        /// <see cref="Tesseract.Pix"/>
        /// object
        /// </returns>
        internal static Pix PreprocessPix(Pix pix) {
            pix = ConvertToGrayscale(pix);
            pix = OtsuImageThresholding(pix);
            return pix;
        }

        /// <summary>
        /// Converts Leptonica
        /// <see cref="Tesseract.Pix"/>
        /// to grayscale.
        /// </summary>
        /// <param name="pix">
        /// 
        /// <see cref="Tesseract.Pix"/>
        /// object to be processed
        /// </param>
        /// <returns>
        /// preprocessed
        /// <see cref="Tesseract.Pix"/>
        /// object
        /// </returns>
        internal static Pix ConvertToGrayscale(Pix pix) {
            if (pix != null)
            {
                int depth = pix.Depth;
                if (depth == 32)
                {
                    return pix.ConvertRGBToGray();
                }
                else
                {
                    LogManager.GetLogger(typeof(iText.Pdfocr.Tesseract4.TesseractOcrUtil))
                        .Info(MessageFormatUtil.Format(Tesseract4LogMessageConstant.CannotConvertImageToGrayscale, depth));
                    return pix;
                }
            }
            else
            {
                return pix;
            }
        }

        /// <summary>
        /// Performs Leptonica Otsu adaptive image thresholding using
        /// <see cref="Net.Sourceforge.Lept4j.Leptonica.PixOtsuAdaptiveThreshold(Tesseract.Pix, int, int, int, int, float, Com.Sun.Jna.Ptr.PointerByReference, Com.Sun.Jna.Ptr.PointerByReference)
        ///     "/>
        /// method.
        /// </summary>
        /// <param name="pix">
        /// 
        /// <see cref="Tesseract.Pix"/>
        /// object to be processed
        /// </param>
        /// <returns>
        /// 
        /// <see cref="Tesseract.Pix"/>
        /// object after thresholding
        /// </returns>
        internal static Pix OtsuImageThresholding(Pix pix) {
            if (pix != null)
            {
                Pix thresholdPix = null;
                if (pix.Depth == 8)
                {
                    thresholdPix = pix.BinarizeOtsuAdaptiveThreshold(pix.Width, pix.Height, 0, 0, 0);
                    if (thresholdPix != null && thresholdPix.Width > 0 && thresholdPix.Height > 0)
                    {
                        return thresholdPix;
                    }
                    else
                    {
                        return pix;
                    }
                }
                else
                {
                    LogManager.GetLogger(typeof(iText.Pdfocr.Tesseract4.TesseractOcrUtil))
                        .Info(MessageFormatUtil.Format(Tesseract4LogMessageConstant.CannotBinarizeImage, pix.Depth));
                    return pix;
                }
            }
            else
            {
                return pix;
            }
        }

        /// <summary>
        /// Destroys
        /// <see cref="Tesseract.Pix"/>
        /// object.
        /// </summary>
        /// <param name="pix">
        /// 
        /// <see cref="Tesseract.Pix"/>
        /// object to be destroyed
        /// </param>
        internal static void DestroyPix(Pix pix) {
            pix.Dispose();
        }

        /// <summary>
        /// Destroys
        /// <see cref="Net.Sourceforge.Lept4j.Pixa"/>
        /// object.
        /// </summary>
        /// <param name="pixa">
        /// 
        /// <see cref="Net.Sourceforge.Lept4j.Pixa"/>
        /// object to be destroyed
        /// </param>
        internal static void DestroyPixa(PixArray pixa) {
            pixa.Dispose();
        }

        /// <summary>Sets tesseract properties.</summary>
        /// <remarks>
        /// Sets tesseract properties.
        /// The following properties are set in this method:
        /// In java: path to tess data, languages, psm
        /// In .Net: psm
        /// This means that other properties have been set during the
        /// initialization of tesseract instance previously or tesseract library
        /// doesn't provide such possibilities in api for .Net or java.
        /// </remarks>
        /// <param name="tesseractInstance">
        /// 
        /// <see cref="Tesseract.TesseractEngine"/>
        /// object
        /// </param>
        /// <param name="tessData">path to tess data directory</param>
        /// <param name="languages">
        /// list of languages in required format
        /// as
        /// <see cref="System.String"/>
        /// </param>
        /// <param name="pageSegMode">
        /// page segmentation mode
        /// <see cref="int?"/>
        /// </param>
        /// <param name="userWordsFilePath">path to a temporary file with user words</param>
        internal static void SetTesseractProperties(TesseractEngine tesseractInstance, String tessData, String languages
            , int? pageSegMode, String userWordsFilePath) {
            if (pageSegMode != null)
            {
                tesseractInstance.DefaultPageSegMode = (PageSegMode)pageSegMode;
            }
        }
        /// <summary>Creates tesseract instance with parameters.</summary>
        /// <remarks>
        /// Creates tesseract instance with parameters.
        /// Method is used to initialize tesseract instance with parameters if it
        /// haven't been initialized yet.
        /// </remarks>
        /// <param name="tessData">path to tess data directory</param>
        /// <param name="languages">
        /// list of languages in required format as
        /// <see cref="System.String"/>
        /// </param>
        /// <param name="isWindows">true is current os is windows</param>
        /// <param name="userWordsFilePath">path to a temporary file with user words</param>
        /// <returns>
        /// initialized
        /// <see cref="Tesseract.TesseractEngine"/>
        /// object
        /// </returns>
        internal static TesseractEngine InitializeTesseractInstance(String tessData, String languages,
            bool isWindows, String userWordsFilePath) {
            return new TesseractEngine(tessData, languages,
                userWordsFilePath != null ? EngineMode.TesseractOnly : EngineMode.Default);
        }

        /// <summary>Creates tesseract instance with parameters.</summary>
        /// <remarks>
        /// Creates tesseract instance with parameters.
        /// Method is used to initialize tesseract instance in constructor (in java).
        /// </remarks>
        /// <param name="isWindows">true is current os is windows</param>
        /// <returns>
        /// initialized
        /// <see cref="Tesseract.TesseractEngine"/>
        /// object
        /// </returns>
        internal static TesseractEngine InitializeTesseractInstance(bool isWindows) {
            return null;
        }

        /// <summary>Returns true if tesseract instance has been already disposed.</summary>
        /// <remarks>
        /// Returns true if tesseract instance has been already disposed.
        /// (used in .net version)
        /// </remarks>
        /// <param name="tesseractInstance">
        /// 
        /// <see cref="Tesseract.TesseractEngine"/>
        /// object to check
        /// </param>
        /// <returns>true if tesseract instance is disposed.</returns>
        internal static bool IsTesseractInstanceDisposed(TesseractEngine tesseractInstance) {
            return tesseractInstance.IsDisposed;
        }

        /// <summary>
        /// Disposes
        /// <see cref="Tesseract.TesseractEngine"/>
        /// instance.
        /// </summary>
        /// <remarks>
        /// Disposes
        /// <see cref="Tesseract.TesseractEngine"/>
        /// instance.
        /// (used in .net version)
        /// </remarks>
        /// <param name="tesseractInstance">
        /// 
        /// <see cref="Tesseract.TesseractEngine"/>
        /// object to dispose
        /// </param>
        internal static void DisposeTesseractInstance(TesseractEngine tesseractInstance) {
            tesseractInstance.Dispose();
        }

        /// <summary>
        /// Converts
        /// <see cref="System.Drawing.Bitmap"/>
        /// to
        /// <see cref="Tesseract.Pix"/>.
        /// </summary>
        /// <param name="bufferedImage">
        /// input image as
        /// <see cref="System.Drawing.Bitmap"/>
        /// </param>
        /// <returns>
        /// Pix result converted
        /// <see cref="Tesseract.Pix"/>
        /// object
        /// </returns>
        internal static Pix ConvertImageToPix(System.Drawing.Bitmap bufferedImage) {
            return PixConverter.ToPix(bufferedImage);
        }

        /// <summary>
        /// Reads
        /// <see cref="Tesseract.Pix"/>
        /// from input file or, if
        /// this is not possible, reads input file as
        /// <see cref="System.Drawing.Bitmap"/>
        /// and then converts to
        /// <see cref="Tesseract.Pix"/>.
        /// </summary>
        /// <param name="inputFile">
        /// input image
        /// <see cref="System.IO.FileInfo"/>
        /// </param>
        /// <returns>
        /// Pix result
        /// <see cref="Tesseract.Pix"/>
        /// object from
        /// input file
        /// </returns>
        internal static Pix ReadPix(FileInfo inputFile) {
            Pix pix = null;
            try {
                System.Drawing.Bitmap bufferedImage = ImagePreprocessingUtil.ReadImageFromFile(inputFile);
                if (bufferedImage != null) {
                    pix = ConvertImageToPix(bufferedImage);
                } else {
                    pix = Tesseract.Pix.LoadFromFile(inputFile.FullName);
                }
            } catch (Exception e) {
                LogManager.GetLogger(typeof(TesseractOcrUtil))
                    .Info(MessageFormatUtil.Format(
                        LogMessageConstant.ReadingImageAsPix,
                        inputFile.FullName,
                        e.Message));
                try {
                    pix = Tesseract.Pix.LoadFromFile(inputFile.FullName);
                } catch (IOException ex) {
                    LogManager.GetLogger(typeof(TesseractOcrUtil))
                        .Info(MessageFormatUtil.Format(
                            Tesseract4LogMessageConstant.CannotReadFile,
                            inputFile.FullName,
                            ex.Message));
                }
            }
            return pix;
        }

        /// <summary>
        /// Converts Leptonica
        /// <see cref="Tesseract.Pix"/>
        /// to
        /// <see cref="System.Drawing.Bitmap"/>
        /// with
        /// <see cref="Net.Sourceforge.Lept4j.ILeptonica.IFF_PNG"/>
        /// image format.
        /// </summary>
        /// <param name="pix">
        /// input
        /// <see cref="Tesseract.Pix"/>
        /// object
        /// </param>
        /// <returns>
        /// result
        /// <see cref="System.Drawing.Bitmap"/>
        /// object
        /// </returns>
        internal static System.Drawing.Bitmap ConvertPixToImage(Pix pix) {
            return PixConverter.ToBitmap(pix);
        }

        /// <summary>Gets current system temporary directory.</summary>
        /// <returns>path to system temporary directory</returns>
        internal static String GetTempDir() {
            return System.IO.Path.GetTempPath();
        }

        /// <summary>
        /// Retrieves list of pages from provided image as list of
        /// <see cref="System.Drawing.Bitmap"/>
        /// , one per page and updates
        /// this list for the image using
        /// <see cref="SetListOfPages(System.Collections.Generic.IList{E})"/>
        /// method.
        /// </summary>
        /// <param name="inputFile">
        /// input image
        /// <see cref="System.IO.FileInfo"/>
        /// </param>
        internal void InitializeImagesListFromTiff(FileInfo inputFile) {
            try
            {
                IList<Pix> pages = new List<Pix>();
                PixArray pixa = PixArray.LoadMultiPageTiffFromFile(inputFile.FullName);
                for (int i = 0; i < pixa.Count; i++)
                {
                    pages.Add(pixa.GetPix(i));
                }
                imagePages = pages;
                DestroyPixa(pixa);
            }
            catch (Exception e)
            {
                LogManager.GetLogger(typeof(TesseractOcrUtil))
                    .Error(MessageFormatUtil.Format(Tesseract4LogMessageConstant.CannotRetrievePagesFromImage, e.Message));
            }
        }

        /// <summary>
        /// Gets list of page of processing image as list of
        /// <see cref="System.Drawing.Bitmap"/>
        /// , one per page.
        /// </summary>
        /// <returns>
        /// result
        /// <see cref="System.Collections.IList{E}"/>
        /// of pages
        /// </returns>
        internal IList<Pix> GetListOfPages() {
            return new List<Pix>(imagePages);
        }

        /// <summary>
        /// Sets list of page of processing image as list of
        /// <see cref="System.Drawing.Bitmap"/>
        /// , one per page.
        /// </summary>
        /// <param name="listOfPages">
        /// list of
        /// <see cref="System.Drawing.Bitmap"/>
        /// for
        /// each page.
        /// </param>
        internal void SetListOfPages(IList<Pix> pages) {
            imagePages = JavaCollectionsUtil.UnmodifiableList<Pix>(pages);
        }

        /// <summary>
        /// Performs ocr for the provided image
        /// and returns result as string in required format.
        /// </summary>
        /// <remarks>
        /// Performs ocr for the provided image
        /// and returns result as string in required format.
        /// (
        /// <see cref="OutputFormat"/>
        /// is used in .Net version,
        /// in java output format should already be set)
        /// </remarks>
        /// <param name="tesseractInstance">
        /// 
        /// <see cref="Tesseract.TesseractEngine"/>
        /// object to perform OCR
        /// </param>
        /// <param name="image">
        /// input
        /// <see cref="System.Drawing.Bitmap"/>
        /// to be processed
        /// </param>
        /// <param name="outputFormat">
        /// selected
        /// <see cref="OutputFormat"/>
        /// for tesseract
        /// </param>
        /// <returns>
        /// result as
        /// <see cref="System.String"/>
        /// in required format
        /// </returns>
        internal String GetOcrResultAsString(TesseractEngine tesseractInstance, System.Drawing.Bitmap image,
            OutputFormat outputFormat) {
            Page page = tesseractInstance.Process(image);
            if (outputFormat.Equals(OutputFormat.HOCR)) {
                return page.GetHOCRText(0);
            } else {
                return page.GetText();
            }
        }

        /// <summary>
        /// Performs ocr for the provided image
        /// and returns result as string in required format.
        /// </summary>
        /// <remarks>
        /// Performs ocr for the provided image
        /// and returns result as string in required format.
        /// (
        /// <see cref="OutputFormat"/>
        /// is used in .Net version, in java output format
        /// should already be set)
        /// </remarks>
        /// <param name="tesseractInstance">
        /// 
        /// <see cref="Tesseract.TesseractEngine"/>
        /// object to perform OCR
        /// </param>
        /// <param name="image">
        /// input image as
        /// <see cref="System.IO.FileInfo"/>
        /// to be
        /// processed
        /// </param>
        /// <param name="outputFormat">
        /// selected
        /// <see cref="OutputFormat"/>
        /// for tesseract
        /// </param>
        /// <returns>
        /// result as
        /// <see cref="System.String"/>
        /// in required format
        /// </returns>
        internal String GetOcrResultAsString(TesseractEngine tesseractInstance, FileInfo image, OutputFormat
             outputFormat) {
            Pix pix = Pix.LoadFromFile(image.FullName);
            return GetOcrResultAsString(tesseractInstance, pix, outputFormat);
        }

        /// <summary>
        /// Performs ocr for the provided image
        /// and returns result as string in required format.
        /// </summary>
        /// <remarks>
        /// Performs ocr for the provided image
        /// and returns result as string in required format.
        /// (
        /// <see cref="OutputFormat"/>
        /// is used in .Net version, in java output format
        /// should already be set)
        /// </remarks>
        /// <param name="tesseractInstance">
        /// 
        /// <see cref="Tesseract.TesseractEngine"/>
        /// object to perform OCR
        /// </param>
        /// <param name="pix">
        /// input image as
        /// <see cref="Tesseract.Pix"/>
        /// to be
        /// processed
        /// </param>
        /// <param name="outputFormat">
        /// selected
        /// <see cref="OutputFormat"/>
        /// for tesseract
        /// </param>
        /// <returns>
        /// result as
        /// <see cref="System.String"/>
        /// in required format
        /// </returns>
        internal String GetOcrResultAsString(TesseractEngine tesseractInstance, Pix pix, OutputFormat outputFormat) {
            Page page = tesseractInstance.Process(pix);
            String result = null;
            if (outputFormat.Equals(OutputFormat.HOCR)) {
                result = page.GetHOCRText(0);
            } else {
                result = page.GetText();
            }
            DestroyPix(pix);
            return result;
        }
    }
}