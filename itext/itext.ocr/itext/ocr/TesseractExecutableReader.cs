using System;
using System.Collections.Generic;
using System.IO;
using Common.Logging;
using iText.IO.Util;

namespace iText.Ocr {
    /// <summary>Tesseract Executable Reader class.</summary>
    /// <remarks>
    /// Tesseract Executable Reader class.
    /// (extends Tesseract Reader class)
    /// <para />
    /// This class provides possibilities to use features of "tesseract"
    /// (optical character recognition engine for various operating systems)
    /// <para />
    /// This class provides possibility to perform OCR, read data from input files
    /// and return contained text in the described format
    /// <para />
    /// This class provides possibilities to set type of current os,
    /// required languages for OCR for input images,
    /// set path to directory with tess data and set path
    /// to the tesseract executable
    /// <para />
    /// Please note that It's assumed that "tesseract" is already
    /// installed in the system
    /// </remarks>
    public class TesseractExecutableReader : TesseractReader {
        /// <summary>TesseractExecutableReader logger.</summary>
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(iText.Ocr.TesseractExecutableReader));

        /// <summary>Path to the script.</summary>
        private String pathToScript;

        /// <summary>Path to the tesseract executable.</summary>
        /// <remarks>
        /// Path to the tesseract executable.
        /// By default it's assumed that "tesseract" already exists in the PATH
        /// </remarks>
        private String pathToExecutable;

        /// <summary>TesseractExecutableReader constructor with path to tess data directory.</summary>
        /// <param name="tessDataPath">String</param>
        public TesseractExecutableReader(String tessDataPath) {
            SetPathToExecutable("tesseract");
            SetOsType(IdentifyOSType());
            SetPathToTessData(tessDataPath);
        }

        /// <summary>
        /// TesseractExecutableReader constructor with path to executable and
        /// path to tess data directory.
        /// </summary>
        /// <param name="executablePath">String</param>
        /// <param name="tessDataPath">String</param>
        public TesseractExecutableReader(String executablePath, String tessDataPath) {
            SetPathToExecutable(executablePath);
            SetOsType(IdentifyOSType());
            SetPathToTessData(tessDataPath);
        }

        /// <summary>
        /// TesseractExecutableReader constructor with path to executable,
        /// list of languages and path to tess data directory.
        /// </summary>
        /// <param name="path">String</param>
        /// <param name="languagesList">List<string></param>
        /// <param name="tessDataPath">String</param>
        public TesseractExecutableReader(String path, String tessDataPath, IList<String> languagesList) {
            SetPathToExecutable(path);
            SetLanguages(JavaCollectionsUtil.UnmodifiableList<String>(languagesList));
            SetPathToTessData(tessDataPath);
            SetOsType(IdentifyOSType());
        }

        /// <summary>Set path to tesseract executable.</summary>
        /// <remarks>
        /// Set path to tesseract executable.
        /// By default it's assumed that "tesseract" already exists in the PATH
        /// </remarks>
        /// <param name="path">String</param>
        public void SetPathToExecutable(String path) {
            pathToExecutable = path;
        }

        /// <summary>Get path to tesseract executable.</summary>
        /// <returns>String</returns>
        public String GetPathToExecutable() {
            return pathToExecutable;
        }

        /// <summary>Set path to script.</summary>
        /// <param name="path">String</param>
        public void SetPathToScript(String path) {
            pathToScript = path;
        }

        /// <summary>Get path to script.</summary>
        /// <returns>String</returns>
        public String GetPathToScript() {
            return pathToScript;
        }

        /// <summary>Perform tesseract OCR.</summary>
        /// <param name="inputImage">- input image file</param>
        /// <param name="outputFiles">
        /// - list of output files (one for each page)
        /// for tesseract executable only the first file is required
        /// </param>
        /// <param name="outputFormat">- output format</param>
        /// <param name="pageNumber">- number of page to be OCRed</param>
        public override void DoTesseractOcr(FileInfo inputImage, IList<FileInfo> outputFiles, IOcrReader.OutputFormat
             outputFormat, int pageNumber) {
            IList<String> command = new List<String>();
            String imagePath = inputImage.FullName;
            try {
                // path to tesseract executable
                AddPathToExecutable(command);
                // path to tess data
                AddTessData(command);
                // validate languages before preprocessing started
                ValidateLanguages(GetLanguagesAsList());
                // preprocess input file if needed and add it
                imagePath = PreprocessImage(inputImage, pageNumber);
                AddInputFile(command, imagePath);
                // output file
                AddOutputFile(command, outputFiles[0], outputFormat);
                // page segmentation mode
                AddPageSegMode(command);
                // add user words if needed
                AddUserWords(command);
                // required languages
                AddLanguages(command);
                if (outputFormat.Equals(IOcrReader.OutputFormat.hocr)) {
                    // path to hocr script
                    SetHocrOutput(command);
                }
                AddPathToScript(command);
                TesseractUtil.RunCommand(command, IsWindows());
            }
            catch (OCRException e) {
                LOGGER.Error("Running tesseract executable failed: " + e);
                throw new OCRException(e.Message);
            }
            finally {
                if (imagePath != null && IsPreprocessingImages() && !inputImage.FullName.Equals(imagePath)) {
                    UtilService.DeleteFile(imagePath);
                }
                if (GetUserWordsFilePath() != null) {
                    UtilService.DeleteFile(GetUserWordsFilePath());
                }
            }
        }

        /// <summary>Add path to tesseract executable.</summary>
        /// <param name="command">List<string></param>
        private void AddPathToExecutable(IList<String> command) {
            // path to tesseract executable cannot be uninitialized
            if (GetPathToExecutable() == null || String.IsNullOrEmpty(GetPathToExecutable())) {
                throw new OCRException(OCRException.CANNOT_FIND_PATH_TO_TESSERACT_EXECUTABLE);
            }
            else {
                command.Add(AddQuotes(GetPathToExecutable()));
            }
        }

        /// <summary>Set hocr output format.</summary>
        /// <param name="command">List<string></param>
        private void SetHocrOutput(IList<String> command) {
            command.Add("-c");
            command.Add("tessedit_create_hocr=1");
        }

        /// <summary>Add path to script.</summary>
        /// <param name="command">List<string></param>
        private void AddPathToScript(IList<String> command) {
            if (GetPathToScript() != null && !String.IsNullOrEmpty(GetPathToScript())) {
                command.Add(AddQuotes(GetPathToScript()));
            }
        }

        /// <summary>Add path to user-words file for tesseract executable.</summary>
        /// <param name="command">List<string></param>
        private void AddUserWords(IList<String> command) {
            if (GetUserWordsFilePath() != null && !String.IsNullOrEmpty(GetUserWordsFilePath())) {
                command.Add("--user-words");
                command.Add(AddQuotes(GetUserWordsFilePath()));
                command.Add("--oem");
                command.Add("0");
            }
        }

        /// <summary>Add path to tess data.</summary>
        /// <param name="command">List<string></param>
        private void AddTessData(IList<String> command) {
            if (GetPathToTessData() != null && !String.IsNullOrEmpty(GetPathToTessData())) {
                command.Add("--tessdata-dir");
                command.Add(AddQuotes(GetTessData()));
            }
        }

        /// <summary>Add select Page Segmentation Mode as parameter.</summary>
        /// <param name="command">List<string></param>
        private void AddPageSegMode(IList<String> command) {
            if (GetPageSegMode() != null) {
                command.Add("--psm");
                command.Add(GetPageSegMode().ToString());
            }
        }

        /// <summary>Add list pf selected languages as parameter.</summary>
        /// <param name="command">List<string></param>
        private void AddLanguages(IList<String> command) {
            if (GetLanguagesAsList().Count > 0) {
                command.Add("-l");
                command.Add(GetLanguagesAsString());
            }
        }

        /// <summary>Preprocess input image (if needed) and add path to this file.</summary>
        /// <param name="command">List<string></param>
        /// <param name="imagePath">path to file</param>
        private void AddInputFile(IList<String> command, String imagePath) {
            command.Add(AddQuotes(imagePath));
        }

        /// <summary>Add path to temporary output file.</summary>
        /// <param name="command">List<string></param>
        /// <param name="outputFile">output file</param>
        /// <param name="outputFormat">output format</param>
        private void AddOutputFile(IList<String> command, FileInfo outputFile, IOcrReader.OutputFormat outputFormat
            ) {
            String extension = outputFormat.Equals(IOcrReader.OutputFormat.hocr) ? ".hocr" : ".txt";
            String fileName = new String(outputFile.FullName.ToCharArray(), 0, outputFile.FullName.IndexOf(extension, 
                StringComparison.Ordinal));
            LOGGER.Info("Temp path: " + outputFile.ToString());
            command.Add(AddQuotes(fileName));
        }

        /// <summary>Surrounds given string with quotes.</summary>
        /// <param name="value">String</param>
        /// <returns>String in quotes</returns>
        private String AddQuotes(String value) {
            return "\"" + value + "\"";
        }

        /// <summary>Preprocess given image if it is needed.</summary>
        /// <param name="inputImage">original input image</param>
        /// <param name="pageNumber">number of page to be OCRed</param>
        /// <returns>path to output image</returns>
        private String PreprocessImage(FileInfo inputImage, int pageNumber) {
            String path = inputImage.FullName;
            if (IsPreprocessingImages()) {
                path = ImageUtil.PreprocessImage(inputImage, pageNumber);
            }
            return path;
        }
    }
}