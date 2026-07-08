#if NET48
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace namasdev.Pdf
{
    public abstract class PdfBase
    {
        private readonly Guid _id;
        private string _imagesTempDirectoryPath;
        private int _imageTempId;

        public PdfBase(string title)
        {
            if (String.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentNullException(nameof(title));
            }

            _id = Guid.NewGuid();

            Title = title;
            FileName = GenerateFileName();
        }

        #region Properties

        #region Public

        public string Title { get; set; }
        public string FileName { get; private set; }

        #endregion

        #region Protected

        protected Section Section { get; private set; }
        protected Document Document { get; private set; }

        #endregion Protected

        #endregion Properties

        #region Methods

        #region Public

        public PdfGenerationResult SaveToPath(string path)
        {
            Exception clearTempImagesException = null;

            try
            {
                var bytes = pGenerateAndGetBytes();
                File.WriteAllBytes(path, bytes);
            }
            finally
            {
                ClearTempImages(
                    out clearTempImagesException);
            }

            return new PdfGenerationResult
            {
                GenerationSuccess = true,
                CleanTempImageException = clearTempImagesException
            };
        }

        public PdfGenerationResult SaveToStream(Stream stream)
        {
            Exception clearTempImagesException = null;

            try
            {
                GenerateAndSaveToStream(stream);
            }
            finally
            {
                ClearTempImages(
                    out clearTempImagesException);
            }

            return new PdfGenerationResult
            {
                GenerationSuccess = true,
                CleanTempImageException = clearTempImagesException
            };
        }

        public byte[] GenerateAndGetBytes(
            out PdfGenerationResult result)
        {
            byte[] bytes = null;
            Exception clearTempImagesException = null;

            try
            {
                bytes = pGenerateAndGetBytes();
            }
            finally
            {
                ClearTempImages(
                    out clearTempImagesException);
            }

            result = new PdfGenerationResult
            {
                GenerationSuccess = bytes != null,
                CleanTempImageException = clearTempImagesException
            };

            return bytes;
        }

        #endregion Public

        #region Protected

        protected void Generate()
        {
            Document = new Document();
            Document.Info.Title = Title;
            //Document.Info.Author = autor;

            Section = Document.AddSection();

            DefinePageStyles();

            GenerateHeader();
            GenerateFooter();
            GenerateContent();
        }

        protected abstract string GetTempImagesDirectoryPath();
        protected abstract void DefinePageStyles();
        protected abstract void GenerateHeader();
        protected abstract void GenerateFooter();
        protected abstract void GenerateContent();

        protected void AddSection()
        {
            Section = Document.AddSection();
        }

        /// <summary>
        /// Downloads the image from the specified URI and saves it temporarily.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="imageExtension"></param>
        /// <returns>Returns the temp image name.</returns>
        protected string SaveTempImage(Uri uri,
            string imageExtension = null)
        {
            CreateImagesTempDirectoryIfNotExists();

            imageExtension = imageExtension ?? Path.GetExtension(uri.LocalPath);

            var imageName = GenerateImageName(imageExtension);
            var imageTempPath = GenerateTempImagePath(imageName);

            using (var webClient = new WebClient())
            {
                try
                {
                    webClient.DownloadFile(uri, imageTempPath);
                }
                catch (Exception)
                {
                    //  nothing to do
                }
            }

            return imageName;
        }

        protected void AddSeparator(
            Section section,
            Unit width, Unit height,
            bool useLine = false, Color? color = null)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            AddSeparator(Section.AddTable(), width, height,
                useLine: useLine,
                color: color);
        }

        protected void AddSeparator(
            HeaderFooter headerFooter, Unit width, Unit height,
            bool useLine = false, Color? color = null)
        {
            if (headerFooter == null)
            {
                throw new ArgumentNullException(nameof(headerFooter));
            }

            AddSeparator(headerFooter.AddTable(), width, height,
                useLine: useLine,
                color: color);
        }

        protected void AddSeparator(
            Table table, Unit width, Unit height,
            bool useLine = false, Color? color = null)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            table.AddColumn(width);
            table.Rows.HeightRule = RowHeightRule.Exactly;
            table.Rows.Height = height;

            var row = table.AddRow();

            if (useLine)
            {
                row.Borders.Bottom.Color = color ?? Colors.Black;
            }

            table.AddRow();
        }

        #endregion Protected

        #region Privados

        private string GenerateFileName()
        {
            return $"{Title}.pdf";
        }

        private void GenerateAndSaveToStream(Stream stream)
        {
            Generate();

            Render()
                .Save(stream, false);
        }

        private MigraDoc.Rendering.PdfDocumentRenderer Render()
        {
            if (Document == null)
            {
                throw new ArgumentNullException(nameof(Document));
            }

            var renderer = new MigraDoc.Rendering.PdfDocumentRenderer();
            renderer.Document = Document;
            renderer.RenderDocument();

            return renderer;
        }

        private byte[] pGenerateAndGetBytes()
        {
            using (var stream = new MemoryStream())
            {
                GenerateAndSaveToStream(stream);
                return stream.ToArray();
            }
        }

        private void CreateImagesTempDirectoryIfNotExists()
        {
            if (!String.IsNullOrWhiteSpace(_imagesTempDirectoryPath))
            {
                return;
            }

            var tempImagesDirectoryPath = GetTempImagesDirectoryPath();
            if (String.IsNullOrWhiteSpace(tempImagesDirectoryPath))
            {
                throw new ArgumentNullException(tempImagesDirectoryPath, "No temp images directory path specified.");
            }

            var path = Path.Combine(tempImagesDirectoryPath, _id.ToString());
            Directory.CreateDirectory(path);

            Document.ImagePath = _imagesTempDirectoryPath = path;

            _imageTempId = 1;
        }

        private bool ClearTempImages(
            out Exception exception)
        {
            exception = null;

            if (!String.IsNullOrWhiteSpace(_imagesTempDirectoryPath))
            {
                try
                {
                    Directory.Delete(_imagesTempDirectoryPath, true);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            Document.ImagePath = _imagesTempDirectoryPath = null;

            return exception == null;
        }

        private string GenerateImageName(string imageExtension)
        {
            if (String.IsNullOrWhiteSpace(imageExtension))
            {
                throw new ArgumentNullException(nameof(imageExtension));
            }

            imageExtension = imageExtension.TrimStart('.');

            var imageName = String.Format("{0:D10}", _imageTempId);
            _imageTempId++;

            return $"{imageName}.{imageExtension}";
        }

        private string GenerateTempImagePath(string imageName)
        {
            return Path.Combine(_imagesTempDirectoryPath, imageName);
        }

        #endregion Privados

        #endregion Methods

        #region Clases internas

        public class ColumnaFormato
        {
            public Unit Width { get; set; }
            public ParagraphFormat Format { get; set; }
            public Borders Borders { get; set; }
            public Shading Shading { get; set; }
            public string Style { get; set; }

            public void Aplicar(Column columna)
            {
                if (columna == null)
                    throw new ArgumentNullException("columna");

                if (Format != null)
                    columna.Format = Format.Clone();

                if (Borders != null)
                    columna.Borders = Borders.Clone();

                if (Shading != null)
                    columna.Shading = Shading.Clone();

                if (!String.IsNullOrWhiteSpace(Style))
                    columna.Style = Style;
            }
        }

        public class FilaFormato
        {
            public Borders Borders { get; set; }
            public ParagraphFormat Format { get; set; }
            public Unit? Height { get; set; }
            public RowHeightRule? HeightRule { get; set; }
            public Shading Shading { get; set; }
            public VerticalAlignment? VerticalAlignment { get; set; }
            public string Style { get; set; }

            public void Aplicar(Row fila)
            {
                if (fila == null)
                    throw new ArgumentNullException("fila");

                if (Borders != null)
                    fila.Borders = Borders.Clone();

                if (Format != null)
                    fila.Format = Format.Clone();

                if (Height.HasValue)
                    fila.Height = Height.Value;

                if (HeightRule.HasValue)
                    fila.HeightRule = HeightRule.Value;

                if (Shading != null)
                    fila.Shading = Shading.Clone();

                if (VerticalAlignment.HasValue)
                    fila.VerticalAlignment = VerticalAlignment.Value;

                if (!String.IsNullOrWhiteSpace(Style))
                    fila.Style = Style;
            }
        }

        public class TablaFormato
        {
            public Borders Borders { get; set; }
            public ParagraphFormat Format { get; set; }
            public Shading Shading { get; set; }
            public string Style { get; set; }

            public void Aplicar(Table table)
            {
                if (table == null)
                    throw new ArgumentNullException("table");

                if (Borders != null)
                    table.Borders = Borders.Clone();

                if (Format != null)
                    table.Format = Format.Clone();

                if (Shading != null)
                    table.Shading = Shading.Clone();

                if (!String.IsNullOrWhiteSpace(Style))
                    table.Style = Style;
            }
        }

        public abstract class TablaBase
        {
            protected TablaBase(Table table)
            {
                if (table == null)
                    throw new ArgumentNullException("table");

                Table = table;
            }

            protected Table Table { get; set; }

            private IEnumerable<ColumnaFormato> FormatoColumnas { get; set; }
            private FilaFormato FormatoFilas { get; set; }

            protected void Inicializar(IEnumerable<ColumnaFormato> columnasFormato, TablaFormato tablaFormato = null, FilaFormato filasFormato = null)
            {
                if (columnasFormato == null || columnasFormato.Count() == 0)
                    throw new ArgumentNullException("columnasFormato");

                FormatoColumnas = columnasFormato;
                FormatoFilas = filasFormato;

                if (tablaFormato != null)
                    tablaFormato.Aplicar(Table);

                foreach (var columnaFormato in columnasFormato)
                {
                    var columna = Table.AddColumn(columnaFormato.Width);
                    columnaFormato.Aplicar(columna);
                }
            }

            protected Row AgregarFila()
            {
                var fila = Table.AddRow();

                if (FormatoFilas != null)
                    FormatoFilas.Aplicar(fila);

                return fila;
            }

            protected void AplicarBordeSoloATabla(Edge edge, BorderStyle borderStyle, Unit unit, Color color)
            {
                Table.SetEdge(0, 0, Table.Columns.Count, Table.Rows.Count, edge, borderStyle, unit, color);
            }

            protected void MantenerFilasEnMismaPagina()
            {
                var cantFilas = Table.Rows.Count;
                if (cantFilas > 0)
                {
                    Table.Rows[0].KeepWith = cantFilas - 1;
                }
            }
        }

        #endregion Clases internas
    }
}
#endif
