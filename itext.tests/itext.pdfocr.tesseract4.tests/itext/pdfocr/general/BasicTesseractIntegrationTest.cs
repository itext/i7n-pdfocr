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
using System.IO;
using System.Text;
using iText.IO.Source;
using iText.IO.Util;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Pdfocr;
using iText.Pdfocr.Tesseract4;
using iText.Test.Attributes;

namespace iText.Pdfocr.General {
    public abstract class BasicTesseractIntegrationTest : IntegrationTestHelper {
        internal AbstractTesseract4OcrEngine tesseractReader;

        public BasicTesseractIntegrationTest(IntegrationTestHelper.ReaderType type) {
            tesseractReader = GetTesseractReader(type);
        }

        [NUnit.Framework.SetUp]
        public virtual void InitTesseractProperties() {
            Tesseract4OcrEngineProperties ocrEngineProperties = new Tesseract4OcrEngineProperties();
            ocrEngineProperties.SetPathToTessData(GetTessDataDirectory());
            tesseractReader.SetTesseract4OcrEngineProperties(ocrEngineProperties);
        }

        [NUnit.Framework.Test]
        public virtual void TestFontColorInMultiPagePdf() {
            String testName = "testFontColorInMultiPagePdf";
            String path = TEST_IMAGES_DIRECTORY + "multîpage.tiff";
            String pdfPath = GetTargetDirectory() + testName + ".pdf";
            FileInfo file = new FileInfo(path);
            tesseractReader.SetTesseract4OcrEngineProperties(tesseractReader.GetTesseract4OcrEngineProperties().SetPreprocessingImages
                (false));
            OcrPdfCreatorProperties ocrPdfCreatorProperties = new OcrPdfCreatorProperties();
            ocrPdfCreatorProperties.SetTextLayerName("Text1");
            Color color = DeviceCmyk.MAGENTA;
            ocrPdfCreatorProperties.SetTextColor(color);
            OcrPdfCreator ocrPdfCreator = new OcrPdfCreator(tesseractReader, ocrPdfCreatorProperties);
            PdfDocument doc = ocrPdfCreator.CreatePdf(JavaCollectionsUtil.SingletonList<FileInfo>(file), GetPdfWriter(
                pdfPath));
            NUnit.Framework.Assert.IsNotNull(doc);
            doc.Close();
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(pdfPath));
            IntegrationTestHelper.ExtractionStrategy strategy = new IntegrationTestHelper.ExtractionStrategy("Text1");
            PdfCanvasProcessor processor = new PdfCanvasProcessor(strategy);
            processor.ProcessPageContent(pdfDocument.GetPage(1));
            Color fillColor = strategy.GetFillColor();
            NUnit.Framework.Assert.AreEqual(fillColor, color);
            pdfDocument.Close();
        }

        [NUnit.Framework.Test]
        public virtual void TestNoisyImage() {
            String path = TEST_IMAGES_DIRECTORY + "tèst/noisy_01.png";
            String expectedOutput1 = "Noisyimage to test Tesseract OCR";
            String expectedOutput2 = "Noisy image to test Tesseract OCR";
            String realOutputHocr = GetTextUsingTesseractFromImage(tesseractReader, new FileInfo(path));
            NUnit.Framework.Assert.IsTrue(realOutputHocr.Equals(expectedOutput1) || realOutputHocr.Equals(expectedOutput2
                ));
        }

        [NUnit.Framework.Test]
        public virtual void TestPantoneImage() {
            String filePath = TEST_IMAGES_DIRECTORY + "pantone_blue.jpg";
            String expected = "";
            String realOutputHocr = GetTextUsingTesseractFromImage(tesseractReader, new FileInfo(filePath));
            NUnit.Framework.Assert.AreEqual(expected, realOutputHocr);
        }

        [NUnit.Framework.Test]
        public virtual void TestDifferentTextStyles() {
            String path = TEST_IMAGES_DIRECTORY + "example_04.png";
            String expectedOutput = "How about a bigger font?";
            TestImageOcrText(tesseractReader, path, expectedOutput);
        }

        [NUnit.Framework.Test]
        public virtual void TestImageWithoutText() {
            String testName = "testImageWithoutText";
            String filePath = TEST_IMAGES_DIRECTORY + "pantone_blue.jpg";
            String pdfPath = GetTargetDirectory() + testName + ".pdf";
            FileInfo file = new FileInfo(filePath);
            OcrPdfCreator ocrPdfCreator = new OcrPdfCreator(tesseractReader);
            ocrPdfCreator.CreatePdf(JavaCollectionsUtil.SingletonList<FileInfo>(file), new PdfWriter(pdfPath)).Close();
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(pdfPath));
            IntegrationTestHelper.ExtractionStrategy strategy = new IntegrationTestHelper.ExtractionStrategy("Text Layer"
                );
            PdfCanvasProcessor processor = new PdfCanvasProcessor(strategy);
            processor.ProcessPageContent(pdfDocument.GetFirstPage());
            pdfDocument.Close();
            NUnit.Framework.Assert.AreEqual("", strategy.GetResultantText());
        }

        [LogMessage(Tesseract4LogMessageConstant.CANNOT_READ_INPUT_IMAGE, Count = 1)]
        [NUnit.Framework.Test]
        public virtual void TestInputInvalidImage() {
            NUnit.Framework.Assert.That(() =>  {
                FileInfo file1 = new FileInfo(TEST_IMAGES_DIRECTORY + "example.txt");
                FileInfo file2 = new FileInfo(TEST_IMAGES_DIRECTORY + "example_05_corrupted.bmp");
                FileInfo file3 = new FileInfo(TEST_IMAGES_DIRECTORY + "numbers_02.jpg");
                tesseractReader.SetTesseract4OcrEngineProperties(tesseractReader.GetTesseract4OcrEngineProperties().SetPathToTessData
                    (GetTessDataDirectory()));
                OcrPdfCreator ocrPdfCreator = new OcrPdfCreator(tesseractReader);
                ocrPdfCreator.CreatePdf(JavaUtil.ArraysAsList(file3, file1, file2, file3), GetPdfWriter());
            }
            , NUnit.Framework.Throws.InstanceOf<Tesseract4OcrException>().With.Message.EqualTo(MessageFormatUtil.Format(Tesseract4OcrException.CANNOT_READ_PROVIDED_IMAGE, new FileInfo(TEST_IMAGES_DIRECTORY + "example.txt").FullName)))
;
        }

        [NUnit.Framework.Test]
        public virtual void TestNonAsciiImagePath() {
            String path = TEST_IMAGES_DIRECTORY + "tèst/noisy_01.png";
            String expectedOutput1 = "Noisyimage to test Tesseract OCR";
            String expectedOutput2 = "Noisy image to test Tesseract OCR";
            String realOutputHocr = GetTextUsingTesseractFromImage(tesseractReader, new FileInfo(path));
            NUnit.Framework.Assert.IsTrue(realOutputHocr.Equals(expectedOutput1) || realOutputHocr.Equals(expectedOutput2
                ));
        }

        [NUnit.Framework.Test]
        public virtual void TestNonAsciiImageName() {
            String path = TEST_IMAGES_DIRECTORY + "nümbérs.jpg";
            String expectedOutput = "619121";
            String realOutputHocr = GetTextUsingTesseractFromImage(tesseractReader, new FileInfo(path));
            NUnit.Framework.Assert.IsTrue(realOutputHocr.Equals(expectedOutput));
        }

        [NUnit.Framework.Test]
        public virtual void TestNullPathToTessData() {
            NUnit.Framework.Assert.That(() =>  {
                FileInfo file = new FileInfo(TEST_IMAGES_DIRECTORY + "spanish_01.jpg");
                tesseractReader.SetTesseract4OcrEngineProperties(tesseractReader.GetTesseract4OcrEngineProperties().SetPathToTessData
                    (null));
                GetTextFromPdf(tesseractReader, file, JavaCollectionsUtil.SingletonList<String>("eng"));
            }
            , NUnit.Framework.Throws.InstanceOf<Tesseract4OcrException>().With.Message.EqualTo(Tesseract4OcrException.PATH_TO_TESS_DATA_DIRECTORY_IS_INVALID))
;
        }

        [NUnit.Framework.Test]
        public virtual void TestPathToTessDataWithoutData() {
            NUnit.Framework.Assert.That(() =>  {
                FileInfo file = new FileInfo(TEST_IMAGES_DIRECTORY + "spanish_01.jpg");
                tesseractReader.SetTesseract4OcrEngineProperties(tesseractReader.GetTesseract4OcrEngineProperties().SetPathToTessData
                    (new FileInfo("test/")));
                GetTextFromPdf(tesseractReader, file, JavaCollectionsUtil.SingletonList<String>("eng"));
            }
            , NUnit.Framework.Throws.InstanceOf<Tesseract4OcrException>().With.Message.EqualTo(Tesseract4OcrException.PATH_TO_TESS_DATA_DIRECTORY_IS_INVALID))
;
        }

        [LogMessage(Tesseract4OcrException.INCORRECT_LANGUAGE)]
        [NUnit.Framework.Test]
        public virtual void TestEmptyPathToTessData() {
            NUnit.Framework.Assert.That(() =>  {
                FileInfo file = new FileInfo(TEST_IMAGES_DIRECTORY + "spanish_01.jpg");
                Tesseract4OcrEngineProperties properties = tesseractReader.GetTesseract4OcrEngineProperties().SetPathToTessData
                    (new FileInfo("."));
                tesseractReader.SetTesseract4OcrEngineProperties(properties);
                GetTextFromPdf(tesseractReader, file);
                NUnit.Framework.Assert.AreEqual(new FileInfo("").FullName, tesseractReader.GetTesseract4OcrEngineProperties
                    ().GetPathToTessData().FullName);
            }
            , NUnit.Framework.Throws.InstanceOf<Tesseract4OcrException>().With.Message.EqualTo(MessageFormatUtil.Format(Tesseract4OcrException.INCORRECT_LANGUAGE, "eng.traineddata", new FileInfo(".").FullName)))
;
        }

        [LogMessage(Tesseract4OcrException.INCORRECT_LANGUAGE, Count = 1)]
        [NUnit.Framework.Test]
        public virtual void TestIncorrectLanguage() {
            NUnit.Framework.Assert.That(() =>  {
                FileInfo file = new FileInfo(TEST_IMAGES_DIRECTORY + "spanish_01.jpg");
                GetTextFromPdf(tesseractReader, file, JavaCollectionsUtil.SingletonList<String>("spa_new"));
            }
            , NUnit.Framework.Throws.InstanceOf<Tesseract4OcrException>().With.Message.EqualTo(MessageFormatUtil.Format(Tesseract4OcrException.INCORRECT_LANGUAGE, "spa_new.traineddata", new FileInfo(LANG_TESS_DATA_DIRECTORY).FullName)))
;
        }

        [LogMessage(Tesseract4OcrException.INCORRECT_LANGUAGE, Count = 1)]
        [NUnit.Framework.Test]
        public virtual void TestListOfLanguagesWithOneIncorrectLanguage() {
            NUnit.Framework.Assert.That(() =>  {
                FileInfo file = new FileInfo(TEST_IMAGES_DIRECTORY + "spanish_01.jpg");
                GetTextFromPdf(tesseractReader, file, JavaUtil.ArraysAsList("spa", "spa_new", "spa_old"));
            }
            , NUnit.Framework.Throws.InstanceOf<Tesseract4OcrException>().With.Message.EqualTo(MessageFormatUtil.Format(Tesseract4OcrException.INCORRECT_LANGUAGE, "spa_new.traineddata", new FileInfo(LANG_TESS_DATA_DIRECTORY).FullName)))
;
        }

        [LogMessage(Tesseract4OcrException.INCORRECT_LANGUAGE, Count = 1)]
        [NUnit.Framework.Test]
        public virtual void TestIncorrectScriptsName() {
            NUnit.Framework.Assert.That(() =>  {
                FileInfo file = new FileInfo(TEST_IMAGES_DIRECTORY + "spanish_01.jpg");
                tesseractReader.SetTesseract4OcrEngineProperties(tesseractReader.GetTesseract4OcrEngineProperties().SetPathToTessData
                    (new FileInfo(SCRIPT_TESS_DATA_DIRECTORY)));
                GetTextFromPdf(tesseractReader, file, JavaCollectionsUtil.SingletonList<String>("English"));
            }
            , NUnit.Framework.Throws.InstanceOf<Tesseract4OcrException>().With.Message.EqualTo(MessageFormatUtil.Format(Tesseract4OcrException.INCORRECT_LANGUAGE, "English.traineddata", new FileInfo(SCRIPT_TESS_DATA_DIRECTORY).FullName)))
;
        }

        [LogMessage(Tesseract4OcrException.INCORRECT_LANGUAGE, Count = 1)]
        [NUnit.Framework.Test]
        public virtual void TestListOfScriptsWithOneIncorrect() {
            NUnit.Framework.Assert.That(() =>  {
                FileInfo file = new FileInfo(TEST_IMAGES_DIRECTORY + "spanish_01.jpg");
                tesseractReader.SetTesseract4OcrEngineProperties(tesseractReader.GetTesseract4OcrEngineProperties().SetPathToTessData
                    (new FileInfo(SCRIPT_TESS_DATA_DIRECTORY)));
                GetTextFromPdf(tesseractReader, file, JavaUtil.ArraysAsList("Georgian", "Japanese", "English"));
            }
            , NUnit.Framework.Throws.InstanceOf<Tesseract4OcrException>().With.Message.EqualTo(MessageFormatUtil.Format(Tesseract4OcrException.INCORRECT_LANGUAGE, "English.traineddata", new FileInfo(SCRIPT_TESS_DATA_DIRECTORY).FullName)))
;
        }

        [NUnit.Framework.Test]
        public virtual void TestTesseract4OcrForOnePageWithTxtFormat() {
            String path = TEST_IMAGES_DIRECTORY + "numbers_01.jpg";
            String expected = "619121";
            FileInfo imgFile = new FileInfo(path);
            FileInfo outputFile = new FileInfo(GetTargetDirectory() + "testTesseract4OcrForOnePage.txt");
            tesseractReader.DoTesseractOcr(imgFile, outputFile, OutputFormat.TXT);
            String result = GetTextFromTextFile(outputFile);
            NUnit.Framework.Assert.IsTrue(result.Contains(expected));
        }

        [NUnit.Framework.Test]
        public virtual void TestSimpleTextOutput() {
            String imgPath = TEST_IMAGES_DIRECTORY + "numbers_01.jpg";
            String expectedOutput = "619121";
            NUnit.Framework.Assert.IsTrue(GetRecognizedTextFromTextFile(tesseractReader, imgPath).Contains(expectedOutput
                ));
        }

        [NUnit.Framework.Test]
        public virtual void TestTxtStringOutput() {
            FileInfo file = new FileInfo(TEST_IMAGES_DIRECTORY + "multîpage.tiff");
            IList<String> expectedOutput = JavaUtil.ArraysAsList("Multipage\nTIFF\nExample\nPage 1", "Multipage\nTIFF\nExample\nPage 2"
                , "Multipage\nTIFF\nExample\nPage 4", "Multipage\nTIFF\nExample\nPage 5", "Multipage\nTIFF\nExample\nPage 6"
                , "Multipage\nTIFF\nExample\nPage /", "Multipage\nTIFF\nExample\nPage 8", "Multipage\nTIFF\nExample\nPage 9"
                );
            String result = tesseractReader.DoImageOcr(file, OutputFormat.TXT);
            foreach (String line in expectedOutput) {
                NUnit.Framework.Assert.IsTrue(iText.IO.Util.StringUtil.ReplaceAll(result, "\r", "").Contains(line));
            }
        }

        [NUnit.Framework.Test]
        public virtual void TestHocrStringOutput() {
            FileInfo file = new FileInfo(TEST_IMAGES_DIRECTORY + "multîpage.tiff");
            IList<String> expectedOutput = JavaUtil.ArraysAsList("Multipage\nTIFF\nExample\nPage 1", "Multipage\nTIFF\nExample\nPage 2"
                , "Multipage\nTIFF\nExample\nPage 4", "Multipage\nTIFF\nExample\nPage 5", "Multipage\nTIFF\nExample\nPage 6"
                , "Multipage\nTIFF\nExample\nPage /", "Multipage\nTIFF\nExample\nPage 8", "Multipage\nTIFF\nExample\nPage 9"
                );
            String result = tesseractReader.DoImageOcr(file, OutputFormat.HOCR);
            foreach (String line in expectedOutput) {
                NUnit.Framework.Assert.IsTrue(iText.IO.Util.StringUtil.ReplaceAll(result, "\r", "").Contains(line));
            }
        }

        [NUnit.Framework.Test]
        public virtual void TestTiffIsDisposed() {
            String testName = "testTiffIsDisposed";
            String path = TEST_IMAGES_DIRECTORY + "numbers_01.tif";
            String tmpPath = System.IO.Path.GetTempFileName();
            String pdfPath = GetTargetDirectory() + testName + ".pdf";
            FileInfo file = new FileInfo(tmpPath);
            File.Copy(path, tmpPath, true);
            OcrPdfCreator ocrPdfCreator = new OcrPdfCreator(tesseractReader);
            NUnit.Framework.Assert.IsTrue(System.GC.TryStartNoGCRegion(8*1024*1024));
            PdfDocument doc = ocrPdfCreator.CreatePdf(JavaCollectionsUtil.SingletonList<FileInfo>(file), GetPdfWriter(
                pdfPath));
            File.Delete(tmpPath);
            System.GC.EndNoGCRegion();
            NUnit.Framework.Assert.IsNotNull(doc);
            doc.Close();
        }

        [NUnit.Framework.Test]
        public virtual void TestTiffIsDisposedWithoutPreprocessing() {
            String testName = "testTiffIsDisposedWithoutPreprocessing";
            String path = TEST_IMAGES_DIRECTORY + "numbers_01.tif";
            String tmpPath = System.IO.Path.GetTempFileName();
            String pdfPath = GetTargetDirectory() + testName + ".pdf";
            FileInfo file = new FileInfo(tmpPath);
            File.Copy(path, tmpPath, true);
            tesseractReader.SetTesseract4OcrEngineProperties(tesseractReader.GetTesseract4OcrEngineProperties().SetPreprocessingImages
                (false));
            OcrPdfCreator ocrPdfCreator = new OcrPdfCreator(tesseractReader);
            NUnit.Framework.Assert.IsTrue(System.GC.TryStartNoGCRegion(8 * 1024 * 1024));
            PdfDocument doc = ocrPdfCreator.CreatePdf(JavaCollectionsUtil.SingletonList<FileInfo>(file), GetPdfWriter(
                pdfPath));
            File.Delete(tmpPath);
            System.GC.EndNoGCRegion();
            NUnit.Framework.Assert.IsNotNull(doc);
            doc.Close();
        }

        [NUnit.Framework.Test]
        public virtual void TestJpegIsDisposed() {
            String testName = "testJpegIsDisposed";
            String path = TEST_IMAGES_DIRECTORY + "numbers_01.jpg";
            String tmpPath = System.IO.Path.GetTempFileName();
            String pdfPath = GetTargetDirectory() + testName + ".pdf";
            FileInfo file = new FileInfo(tmpPath);
            File.Copy(path, tmpPath, true);
            OcrPdfCreator ocrPdfCreator = new OcrPdfCreator(tesseractReader);
            NUnit.Framework.Assert.IsTrue(System.GC.TryStartNoGCRegion(8 * 1024 * 1024));
            PdfDocument doc = ocrPdfCreator.CreatePdf(JavaCollectionsUtil.SingletonList<FileInfo>(file), GetPdfWriter(
                pdfPath));
            File.Delete(tmpPath);
            System.GC.EndNoGCRegion();
            NUnit.Framework.Assert.IsNotNull(doc);
            doc.Close();
        }

        /// <summary>Parse text from image and compare with expected.</summary>
        private void TestImageOcrText(AbstractTesseract4OcrEngine tesseractReader, String path, String expectedOutput
            ) {
            FileInfo ex1 = new FileInfo(path);
            String realOutputHocr = GetTextUsingTesseractFromImage(tesseractReader, ex1);
            NUnit.Framework.Assert.IsTrue(realOutputHocr.Contains(expectedOutput));
        }

        /// <summary>Parse text from given image using tesseract.</summary>
        private String GetTextUsingTesseractFromImage(IOcrEngine tesseractReader, FileInfo file) {
            int page = 1;
            IDictionary<int, IList<TextInfo>> data = tesseractReader.DoImageOcr(file);
            IList<TextInfo> pageText = data.Get(page);
            if (pageText == null || pageText.Count == 0) {
                pageText = new List<TextInfo>();
                TextInfo textInfo = new TextInfo();
                textInfo.SetBboxRect(new Rectangle(0, 0, 0, 0));
                textInfo.SetText("");
                pageText.Add(textInfo);
            }
            return GetTextFromPage(pageText);
        }

        /// <summary>Concatenates provided text items to one string.</summary>
        private String GetTextFromPage(IList<TextInfo> pageText) {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (TextInfo text in pageText) {
                stringBuilder.Append(text.GetText());
                stringBuilder.Append(" ");
            }
            return stringBuilder.ToString().Trim();
        }

        /// <summary>Create pdfWriter.</summary>
        private PdfWriter GetPdfWriter() {
            return new PdfWriter(new ByteArrayOutputStream(), new WriterProperties().AddUAXmpMetadata());
        }
    }
}
