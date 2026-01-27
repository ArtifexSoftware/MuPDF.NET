using System;
using System.Collections.Generic;
using System.IO;
using MuPDF.NET;
using MuPDF.NET.LLM.Llama;

namespace MuPDF.NET.LLM.Test
{
    [TestFixture]
    public class PDFMarkdownReaderTest : LLMTestBase
    {
        [Test]
        public void Constructor_WithoutMetaFilter_CreatesReader()
        {
            var reader = new PDFMarkdownReader();
            Assert.That(reader, Is.Not.Null);
            Assert.That(reader.MetaFilter, Is.Null);
        }

        [Test]
        public void Constructor_WithMetaFilter_CreatesReader()
        {
            Func<Dictionary<string, object>, Dictionary<string, object>> filter = 
                (meta) => meta;
            
            var reader = new PDFMarkdownReader(filter);
            Assert.That(reader, Is.Not.Null);
            Assert.That(reader.MetaFilter, Is.EqualTo(filter));
        }

        [Test]
        public void LoadData_WithNullFilePath_ThrowsArgumentNullException()
        {
            var reader = new PDFMarkdownReader();
            Assert.Throws<ArgumentNullException>(() =>
            {
                reader.LoadData(null);
            });
        }

        [Test]
        public void LoadData_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            var reader = new PDFMarkdownReader();
            Assert.Throws<FileNotFoundException>(() =>
            {
                reader.LoadData("nonexistent.pdf");
            });
        }

        [Test]
        public void LoadData_WithValidFile_ReturnsDocuments()
        {
            var reader = new PDFMarkdownReader();
            string filePath = GetResourcePath("columns.pdf");
            
            var docs = reader.LoadData(filePath);
            
            Assert.That(docs, Is.Not.Null);
            Assert.That(docs.Count, Is.GreaterThan(0));
        }

        [Test]
        public void LoadData_WithValidFile_ReturnsDocumentsWithText()
        {
            var reader = new PDFMarkdownReader();
            string filePath = GetResourcePath("columns.pdf");
            
            var docs = reader.LoadData(filePath);
            
            Assert.That(docs.Count, Is.GreaterThan(0));
            foreach (var doc in docs)
            {
                Assert.That(doc, Is.Not.Null);
                Assert.That(doc.Text, Is.Not.Null);
                Assert.That(doc.ExtraInfo, Is.Not.Null);
            }
        }

        [Test]
        public void LoadData_WithExtraInfo_IncludesExtraInfo()
        {
            var reader = new PDFMarkdownReader();
            string filePath = GetResourcePath("columns.pdf");
            var extraInfo = new Dictionary<string, object>
            {
                { "custom_key", "custom_value" }
            };
            
            var docs = reader.LoadData(filePath, extraInfo: extraInfo);
            
            Assert.That(docs.Count, Is.GreaterThan(0));
            Assert.That(docs[0].ExtraInfo.ContainsKey("custom_key"), Is.True);
            Assert.That(docs[0].ExtraInfo["custom_key"], Is.EqualTo("custom_value"));
        }

        [Test]
        public void LoadData_WithMetaFilter_AppliesFilter()
        {
            bool filterCalled = false;
            Func<Dictionary<string, object>, Dictionary<string, object>> filter = 
                (meta) =>
                {
                    filterCalled = true;
                    meta["filtered"] = true;
                    return meta;
                };
            
            var reader = new PDFMarkdownReader(filter);
            string filePath = GetResourcePath("columns.pdf");
            
            var docs = reader.LoadData(filePath);
            
            Assert.That(filterCalled, Is.True);
            Assert.That(docs.Count, Is.GreaterThan(0));
            Assert.That(docs[0].ExtraInfo.ContainsKey("filtered"), Is.True);
            Assert.That(docs[0].ExtraInfo["filtered"], Is.EqualTo(true));
        }

        [Test]
        public void LoadData_WithLoadKwargs_RespectsKwargs()
        {
            var reader = new PDFMarkdownReader();
            string filePath = GetResourcePath("columns.pdf");
            var loadKwargs = new Dictionary<string, object>
            {
                { "force_text", true },
                { "write_images", false },
                { "embed_images", false }
            };
            
            var docs = reader.LoadData(filePath, loadKwargs: loadKwargs);
            
            Assert.That(docs, Is.Not.Null);
            Assert.That(docs.Count, Is.GreaterThan(0));
        }

        [Test]
        public void LoadData_WithStringPath_Works()
        {
            var reader = new PDFMarkdownReader();
            string filePath = GetResourcePath("columns.pdf");
            
            var docs = reader.LoadData(filePath);
            
            Assert.That(docs, Is.Not.Null);
        }

        [Test]
        public void LoadData_IncludesPageMetadata()
        {
            var reader = new PDFMarkdownReader();
            string filePath = GetResourcePath("columns.pdf");
            
            var docs = reader.LoadData(filePath);
            
            Assert.That(docs.Count, Is.GreaterThan(0));
            Assert.That(docs[0].ExtraInfo.ContainsKey("page"), Is.True);
            Assert.That(docs[0].ExtraInfo.ContainsKey("total_pages"), Is.True);
            Assert.That(docs[0].ExtraInfo.ContainsKey("file_path"), Is.True);
        }
    }
}
