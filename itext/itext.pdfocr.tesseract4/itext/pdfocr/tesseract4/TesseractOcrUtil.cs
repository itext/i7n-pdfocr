/*
    This file is part of the iText (R) project.
    Copyright (c) 1998-2021 iText Group NV
    Authors: iText Software.

    This program is offered under a commercial and under the AGPL license.
    For commercial licensing, contact us at https://itextpdf.com/sales.  For AGPL licensing, see below.

    AGPL licensing:
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Common.Logging;
using iText.IO.Image;
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

        /// <summary>Rotation constants.</summary>
        private const int ROTATION_0 = 0;
        private const int ROTATION_90 = 90;
        private const int ROTATION_180 = 180;
        private const int ROTATION_270 = 270;
        private const int EXIF_ROTATION_0 = 1;
        private const int EXIF_ROTATION_90 = 6;
        private const int EXIF_ROTATION_180 = 3;
        private const int EXIF_ROTATION_270 = 8;

        /// <summary>List of pages of the image that is being processed.</summary>
        private IList<Bitmap> imagePages = new List<Bitmap>();

        /// <summary>
        /// Creates a new
        /// <see cref="TesseractOcrUtil"/>
        /// instance.
        /// </summary>
        internal TesseractOcrUtil() {
        }

        /// <summary>
        /// Reads required page from provided tiff image.
        /// Note that rotation is always applied when image read.
        /// </summary>
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
            // copy selected pix form pixa
            Pix pix = null;
            Bitmap img = GetImagePage(inputFile, pageNumber);
            if (img != null)
            {
                pix = ReadPix(img);
            }
            // return required page to be preprocessed
            return pix;
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
        /// <param name="imagePreprocessingOptions">
        /// 
        /// <see cref="ImagePreprocessingOptions"/>
        /// </param>
        /// <returns>
        /// preprocessed
        /// <see cref="Tesseract.Pix"/>
        /// object
        /// </returns>
        internal static Pix PreprocessPix(Pix pix, ImagePreprocessingOptions imagePreprocessingOptions) {
            Pix pix1 = ConvertToGrayscale(pix);
            pix1 = OtsuImageThresholding(pix1, imagePreprocessingOptions);
            return pix1;
        }

        /// <summary>
        /// Converts Leptonica
        /// <see cref="Tesseract.Pix"/>
        /// to grayscale.
        /// </summary>
        /// <remarks>
        ///  In .Net image is converted only if this is 32bpp image. In java image is
        ///  converted anyway using different Leptonica methods depending on
        ///  image depth.
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
                    LogManager.GetLogger(typeof(TesseractOcrUtil))
                        .Info(MessageFormatUtil.Format(Tesseract4LogMessageConstant.CANNOT_CONVERT_IMAGE_TO_GRAYSCALE, depth));
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
        /// <param name="imagePreprocessingOptions">
        /// 
        /// <see cref="ImagePreprocessingOptions"/>
        /// </param>
        /// <returns>
        /// 
        /// <see cref="Tesseract.Pix"/>
        /// object after thresholding
        /// </returns>
        internal static Pix OtsuImageThresholding(Pix pix, ImagePreprocessingOptions imagePreprocessingOptions) {
            if (pix != null)
            {
                Pix thresholdPix = null;
                if (pix.Depth == 8)
                {
                    thresholdPix = pix.BinarizeOtsuAdaptiveThreshold(
                        GetOtsuAdaptiveThresholdTileSize(pix.Width,
                            imagePreprocessingOptions.GetTileWidth()),
                        GetOtsuAdaptiveThresholdTileSize(pix.Height,
                            imagePreprocessingOptions.GetTileHeight()),
                        GetOtsuAdaptiveThresholdSmoothingTileSize(pix.Width,
                            imagePreprocessingOptions.IsSmoothTiling()),
                        GetOtsuAdaptiveThresholdSmoothingTileSize(pix.Height,
                            imagePreprocessingOptions.IsSmoothTiling()),
                        0);
                    if (thresholdPix != null && thresholdPix.Width > 0 && thresholdPix.Height > 0)
                    {
                        DestroyPix(pix);
                        return thresholdPix;
                    }
                    else
                    {
                        LogManager.GetLogger(typeof(TesseractOcrUtil))
                            .Info(MessageFormatUtil.Format(Tesseract4LogMessageConstant.CANNOT_BINARIZE_IMAGE, pix.Depth));
                        DestroyPix(thresholdPix);
                        return pix;
                    }
                }
                else
                {
                    LogManager.GetLogger(typeof(TesseractOcrUtil))
                        .Info(MessageFormatUtil.Format(Tesseract4LogMessageConstant.CANNOT_BINARIZE_IMAGE, pix.Depth));
                    return pix;
                }
            }
            else
            {
                return pix;
            }
        }

        /// <summary>
        /// Gets adaptive threshold tile size.
        /// </summary>
        internal static int GetOtsuAdaptiveThresholdTileSize(int imageSize, int tileSize)
        {
            if (tileSize == 0)
            {
                return imageSize;
            }
            else
            {
                return tileSize;
            }
        }

        /// <summary>
        /// Gets adaptive threshold smoothing tile size.
        /// Can be either equal to page size or 0.
        /// </summary>
        internal static int GetOtsuAdaptiveThresholdSmoothingTileSize(int imageSize, bool smoothTiling)
        {
            if (smoothTiling)
            {
                return imageSize;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets an integer pixel in the default RGB color model.
        /// </summary>
        internal static int GetImagePixelColor(System.Drawing.Bitmap image, int x, int y)
        {
            return image.GetPixel(x, y).ToArgb();
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
            if (pix != null) {
                pix.Dispose();
            }
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
        /// In this method in java 'tessData', 'languages' and 'userWordsFilePath'
        /// properties are unused as they will be set using setters in
        /// <see cref="SetTesseractProperties"/> method. In .Net all these properties
        /// are needed to be provided in tesseract constructor in order to
        /// initialize tesseract instance.Thus, tesseract initialization takes
        /// place in <see cref="Tesseract4LibOcrEngine.Tesseract4LibOcrEngine(Tesseract4OcrEngineProperties)"/> constructor in
        /// java, but in .Net it happens only after all properties are validated,
        /// i.e. just before OCR process.
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
        internal static TesseractEngine InitializeTesseractInstance(bool isWindows, String tessData, String languages,
            String userWordsFilePath) {
            if (tessData == null && languages == null && userWordsFilePath == null) {
                // this means that properties have not been validated yet
                // and Tesseract Engine will be initialized later, just before OCR process itself
                return null;
            } else
            {
                try
                {
                    return new TesseractEngine(tessData, languages,
                        userWordsFilePath != null ? EngineMode.TesseractOnly : EngineMode.Default);
                }
                catch (Exception e)
                {
                    throw new Tesseract4OcrException(isWindows ?
                        Tesseract4OcrException.TESSERACT_LIB_NOT_INSTALLED_WIN :
                        Tesseract4OcrException.TESSERACT_LIB_NOT_INSTALLED, e);
                }
            }
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
            // tesseract restrictions
            // should be improved in further tesseract versions
            if (pix != null)
            {
                return new PixToBitmapConverter().Convert(pix);
            }
            return null;
        }

        /// <summary>Gets current system temporary directory.</summary>
        /// <param name="name">file name</param>
        /// <param name="suffix">file suffix</param>
        /// <returns>path to system temporary directory</returns>
        internal static String GetTempFilePath(string name, string suffix) {
            return System.IO.Path.GetTempPath() + name + suffix;
        }

        /// <summary>Returns parent directory for the passed path.</summary>
        /// <param name="path">path path to file</param>
        /// <returns>parent directory where the file is located</returns>
        internal static String GetParentDirectory(string path)
        {
            return Directory.GetParent(path).FullName;
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
        internal void InitializeImagesListFromTiff(FileInfo inputFile)
        {
            try
            {
                Image originalImage = Image.FromFile(inputFile.FullName);
                List<Bitmap> bitmapList = new List<Bitmap>();
                var xResolution = originalImage.HorizontalResolution;
                var yResolution = originalImage.VerticalResolution;

                FrameDimension frameDimension = new FrameDimension(originalImage.FrameDimensionsList[0]);
                int frameCount = originalImage.GetFrameCount(FrameDimension.Page);
                for (int i = 0; i < frameCount; ++i)
                {
                    originalImage.SelectActiveFrame(frameDimension, i);
                    Bitmap temp = new Bitmap(originalImage);
                    temp.SetResolution(2 * xResolution, 2 * yResolution);
                    bitmapList.Add(temp);
                }
                originalImage.Dispose();
                SetListOfPages(bitmapList);
            } catch (Exception e)
            {
                LogManager.GetLogger(typeof(TesseractOcrUtil))
                    .Error(MessageFormatUtil.Format(
                        Tesseract4LogMessageConstant.CANNOT_RETRIEVE_PAGES_FROM_IMAGE,
                        inputFile.FullName,
                        e.Message));
            }
        }

        /// <summary>
        /// Gets requested image page from the provided image.
        /// </summary>
        /// <param name="inputFile">
        /// input image
        /// </param>
        /// <param name="page">
        /// requested image page
        /// </param>
        /// <returns>
        /// requested image page as a
        /// <see cref="System.Drawing.Bitmap"/>
        /// </returns>
        internal static Bitmap GetImagePage(FileInfo input, int page)
        {
            Bitmap img = null;
            try
            {
                Image image = Image.FromFile(input.FullName);
                int pages = image.GetFrameCount(FrameDimension.Page);
                if (page >= pages)
                {
                    LogManager.GetLogger(typeof(TesseractOcrUtil))
                        .Warn(MessageFormatUtil.Format(
                            Tesseract4LogMessageConstant.PAGE_NUMBER_IS_INCORRECT,
                            page,
                            input.FullName));
                    return null;
                }
                image.SelectActiveFrame(FrameDimension.Page, page);
                img = new Bitmap(image);
                image.Dispose();
            } catch (Exception e)
            {
                LogManager.GetLogger(typeof(TesseractOcrUtil))
                    .Error(MessageFormatUtil.Format(
                        Tesseract4LogMessageConstant.CANNOT_RETRIEVE_PAGES_FROM_IMAGE,
                        input.FullName,
                        e.Message));
            }
            return img;
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
        internal IList<Bitmap> GetListOfPages() {
            return new List<Bitmap>(imagePages);
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
        internal void SetListOfPages(IList<Bitmap> pages) {
            imagePages = JavaCollectionsUtil.UnmodifiableList<Bitmap>(pages);
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
            String result = null;
            if (image != null) {
                Page page = null;
                try {
                    page = tesseractInstance.Process(image);
                    if (outputFormat.Equals(OutputFormat.HOCR)) {
                        result = page.GetHOCRText(0);
                    } else {
                        result = page.GetText();
                    }
                } finally {
                    if (page != null) {
                        page.Dispose();
                    }
                }
            }
            return result;
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
            if (image != null)
            {
                Pix pix = Pix.LoadFromFile(image.FullName);
                return GetOcrResultAsString(tesseractInstance, pix, outputFormat);
            }
            return null;
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
            String result = null;
            if (pix != null) {
                Page page = null;
                try
                {
                    page = tesseractInstance.Process(pix);
                    if (outputFormat.Equals(OutputFormat.HOCR))
                    {
                        result = page.GetHOCRText(0);
                    }
                    else
                    {
                        result = page.GetText();
                    }
                } finally
                {
                    if (page != null) {
                        page.Dispose();
                    }
                    DestroyPix(pix);
                }
            }
            return result;
        }

        /// <summary>
        /// Saves passed <see cref="System.Drawing.Bitmap"/> to given path
        /// </summary>
        /// <param name="tmpFileName">
        /// provided file path to save the <see cref="System.Drawing.Bitmap"/>
        /// </param>
        /// <param name="image">
        /// provided <see cref="System.Drawing.Bitmap"/> to be saved
        /// </param>
        internal static void SaveImageToTempPngFile(string tmpFileName, Bitmap image)
        {
            if (image != null) {
                try
                {
                    image.Save(tmpFileName, System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (Exception e)
                {
                    LogManager.GetLogger(typeof(TesseractOcrUtil)).Error(MessageFormatUtil.Format(
                            Tesseract4LogMessageConstant.CANNOT_PROCESS_IMAGE,
                            e.Message));
                }
            }

        }

        /// <summary>
        /// Saves passed<see cref="Tesseract.Pix"/> to given path
        /// </summary>
        /// <param name="filename">
        /// provided file path to save the <see cref="Tesseract.Pix"/>
        /// </param>
        /// <param name="pix">
        /// provided <see cref="Tesseract.Pix"/> to be saved
        /// </param>
        internal static void SavePixToPngFile(string filename, Pix pix)
        {
            if (pix != null)
            {
                try
                {
                    pix.Save(filename, Tesseract.ImageFormat.Png);
                }
                catch (Exception e)
                {
                    LogManager.GetLogger(typeof(TesseractOcrUtil)).Info(MessageFormatUtil.Format(
                            Tesseract4LogMessageConstant.CANNOT_PROCESS_IMAGE,
                            e.Message));
                }
            }
        }

        /// <summary>
        /// Create temporary copy of input file to avoid
        /// issue with tesseract and different encodings
        /// in the path.
        /// </summary>
        /// <param name="src">
        /// path to the source image
        /// </param>
        /// <param name="dst">
        /// path to the source image
        /// </param>
        internal static void CreateTempFileCopy(string src, string dst)
        {
            File.Copy(src, dst, true);
        }


        /// <summary>
        /// Read
        /// <see cref="Tesseract.Pix"/>
        /// from 
        /// <see cref="System.Drawing.Bitmap"/>.
        /// Note that rotation is always applied when image read.
        /// </summary>
        /// <param name="bufferedImage">
        /// image
        /// <see cref="System.Drawing.Bitmap"/>
        /// to read from
        /// </param>
        /// <returns>
        /// Pix result
        /// <see cref="Tesseract.Pix"/>
        /// </returns>
        internal static Pix ReadPix(System.Drawing.Bitmap bufferedImage)
        {
            if (bufferedImage != null)
            {
                Pix pix = new BitmapToPixConverter().Convert(bufferedImage);
                if (pix != null)
                {
                    int rotation = ReadRotationFromMetadata((System.Drawing.Image)bufferedImage);
                    pix = Rotate(pix, rotation);
                }
                return pix;
            }
            return null;
        }

        /// <summary>
        /// Read
        /// <see cref="Tesseract.Pix"/>
        /// from 
        /// <see cref="System.IO.FileInfo"/>.
        /// Note that rotation is always applied when image read.
        /// </summary>
        /// <param name="inputFile">
        /// input file
        /// <see cref="System.IO.FileInfo"/>
        /// to read from
        /// </param>
        /// <returns>
        /// Pix result
        /// <see cref="Tesseract.Pix"/>
        /// </returns>
        internal static Pix ReadPix(FileInfo inputFile)
        {
            Pix pix = null;
            try
            {
                pix = Pix.LoadFromFile(inputFile.FullName);
            }
            catch (Exception e)
            {
                // NOSONAR
                LogManager.GetLogger(typeof(TesseractOcrUtil)).Error(MessageFormatUtil.Format
                    (Tesseract4LogMessageConstant.CANNOT_READ_INPUT_IMAGE, e.Message));
            }
            if (pix != null)
            {
                int rotation = DetectRotation(inputFile);
                pix = Rotate(pix, rotation);
            }
            return pix;
        }

        /// <summary>
        /// Read
        /// <see cref="Tesseract.Pix"/>
        /// from byte array.
        /// Note that rotation is always applied when image read.
        /// </summary>
        /// <param name="imageBytes">
        /// image bytes to read from
        /// </param>
        /// <returns>
        /// Pix result
        /// <see cref="Tesseract.Pix"/>
        /// </returns>
        internal static Pix ReadPix(byte[] imageBytes)
        {
            try
            {
                System.Drawing.Bitmap bufferedImage = (System.Drawing.Bitmap)
                    ((new ImageConverter()).ConvertFrom(imageBytes));
                return ReadPix(bufferedImage);
            }
            catch (Exception e)
            {
                // NOSONAR
                LogManager.GetLogger(typeof(TesseractOcrUtil)).Error(MessageFormatUtil.Format
                    (Tesseract4LogMessageConstant.CANNOT_READ_INPUT_IMAGE, e.Message));
                return null;
            }
        }

        /// <summary>
        /// Detect rotation specified by image metadata.
        /// </summary>
        /// <param name="inputFile">
        /// file to detect rotation
        /// </param>
        /// <returns>
        /// image rotation as specified in metadata
        /// </returns>
        internal static int DetectRotation(FileInfo inputFile)
        {
            try
            {
                using (System.Drawing.Image image = System.Drawing.Image.FromFile(inputFile.FullName))
                    return ReadRotationFromMetadata(image);
            }
            catch (Exception e)
            {
                // NOSONAR
                LogManager.GetLogger(typeof(TesseractOcrUtil)).Error(MessageFormatUtil.Format
                    (Tesseract4LogMessageConstant.CANNOT_READ_INPUT_IMAGE, e.Message));
                return ROTATION_0;
            }
        }

        /// <summary>
        /// Detect rotation specified by image metadata.
        /// </summary>
        /// <param name="imageData">
        /// imageData to detect rotation
        /// </param>
        /// <returns>
        /// image rotation as specified in metadata
        /// </returns>
        internal static int DetectRotation(ImageData imageData)
        {
            return DetectRotation(imageData.GetData());
        }

        /// <summary>
        /// Detect rotation specified by image metadata.
        /// </summary>
        /// <param name="imageBytes">
        /// image data to detect rotation
        /// </param>
        /// <returns>
        /// image rotation as specified in metadata
        /// </returns>
        internal static int DetectRotation(byte[] imageBytes)
        {
            try
            {
                System.Drawing.Bitmap bufferedImage = (System.Drawing.Bitmap)
                    ((new ImageConverter()).ConvertFrom(imageBytes));
                return ReadRotationFromMetadata(bufferedImage);
            }
            catch (Exception e)
            {
                // NOSONAR
                LogManager.GetLogger(typeof(TesseractOcrUtil)).Error(MessageFormatUtil.Format
                    (Tesseract4LogMessageConstant.CANNOT_READ_INPUT_IMAGE, e.Message));
                return ROTATION_0;
            }
        }

        /// <summary>
        /// Reads image orientation from metadata and converts to rotation.
        /// </summary>
        /// <param name="image">
        /// image metadata
        /// </param>
        /// <returns>
        /// rotation
        /// </returns>
        internal static int ReadRotationFromMetadata(System.Drawing.Image image)
        {
            const int exifOrientationID = 0x112;

            if (!image.PropertyIdList.Contains(exifOrientationID))
                return ROTATION_0;

            PropertyItem prop = image.GetPropertyItem(exifOrientationID);
            int orientation = BitConverter.ToUInt16(prop.Value, 0);
            switch (orientation)
            {
                case EXIF_ROTATION_0:
                    return ROTATION_0;
                case EXIF_ROTATION_90:
                    return ROTATION_90;
                case EXIF_ROTATION_180:
                    return ROTATION_180;
                case EXIF_ROTATION_270:
                    return ROTATION_270;
                default:
                    LogManager.GetLogger(typeof(TesseractOcrUtil)).Warn(MessageFormatUtil.Format(
                            Tesseract4LogMessageConstant.UNSUPPORTED_EXIF_ORIENTATION_VALUE,
                            orientation));
                    return ROTATION_0;
            }
        }

        /// <summary>
        /// Rotates image by specified angle.
        /// </summary>
        /// <param name="pix">
        /// image source represented by
        /// <see cref="Tesseract.Pix"/>
        /// </param>
        /// <param name="rotation">
        /// to rotate image at
        /// </param>
        /// <returns>
        /// rotated image, if rotation differs from 0
        /// </returns>
        internal static Pix Rotate(Pix pix, int rotation)
        {
            switch (rotation)
            {
                case ROTATION_90:
                    return pix.Rotate90(1);
                case ROTATION_180:
                    return pix.Rotate90(1).Rotate90(1);
                case ROTATION_270:
                    return pix.Rotate90(-1);
                default:
                    return pix;
            }
        }

        /// <summary>
        /// Detects and applies rotation to image.
        /// </summary>
        /// <param name="imageData">
        /// source image to rotate if needed
        /// </param>
        /// <returns>
        /// rotated image, if rotation differs from 0
        /// </returns>
        internal static ImageData ApplyRotation(ImageData imageData)
        {
            Pix pix = ReadPix(imageData.GetData());
            if (pix == null)
            {
                return imageData;
            }
            else
            {
                ImageData newImageData = imageData;
                MemoryStream stream = new MemoryStream();
                try
                {
                    System.Drawing.Bitmap bitmap = ConvertPixToImage(pix);
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    byte[] byteArray = stream.GetBuffer();
                    newImageData = ImageDataFactory.Create(byteArray);
                }
                finally
                {
                    stream.Close();
                    DestroyPix(pix);
                }
                return newImageData;
            }
        }
    }
}
