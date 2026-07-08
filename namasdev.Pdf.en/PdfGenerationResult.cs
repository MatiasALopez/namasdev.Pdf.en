using System;

namespace namasdev.Pdf
{
    public class PdfGenerationResult
    {
        public bool GenerationSuccess { get; set; }
        
        public bool CleanTempImagesSuccess 
        {
            get { return CleanTempImageException == null; }
        }

        public Exception CleanTempImageException { get; set; }
    }
}
