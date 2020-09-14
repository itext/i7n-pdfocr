/*
This file is part of the iText (R) project.
Copyright (c) 1998-2020 iText Group NV
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
using System.IO;
using iText.Pdfocr.Helpers;
using iText.Test;
using iText.Test.Attributes;

namespace iText.Pdfocr {
    public class PdfInputImageTest : ExtendedITextTest {
        [LogMessage(PdfOcrLogMessageConstant.CANNOT_READ_INPUT_IMAGE, Count = 1)]
        [NUnit.Framework.Test]
        public virtual void TestCorruptedImage() {
            NUnit.Framework.Assert.That(() =>  {
                FileInfo file = new FileInfo(PdfHelper.GetImagesTestDirectory() + "corrupted.jpg");
                String realOutput = PdfHelper.GetTextFromPdf(file, "testCorruptedImage");
                NUnit.Framework.Assert.IsNotNull(realOutput);
                NUnit.Framework.Assert.AreEqual("", realOutput);
            }
            , NUnit.Framework.Throws.InstanceOf<OcrException>())
;
        }

        [NUnit.Framework.Test]
        public virtual void TestNonexistentImage() {
            NUnit.Framework.Assert.That(() =>  {
                FileInfo file = new FileInfo(PdfHelper.GetImagesTestDirectory() + "corrupted");
                String realOutput = PdfHelper.GetTextFromPdf(file, "testNonexistentImage");
                NUnit.Framework.Assert.IsNotNull(realOutput);
                NUnit.Framework.Assert.AreEqual("", realOutput);
            }
            , NUnit.Framework.Throws.InstanceOf<IO.IOException>())
;
        }

        [NUnit.Framework.Test]
        public virtual void TestJpegWithoutExtension() {
            FileInfo file = new FileInfo(PdfHelper.GetImagesTestDirectory() + "numbers_01");
            String realOutput = PdfHelper.GetTextFromPdf(file, "testJpegWithoutExtension");
            NUnit.Framework.Assert.IsNotNull(realOutput);
            NUnit.Framework.Assert.AreEqual("619121", realOutput);
        }
    }
}
