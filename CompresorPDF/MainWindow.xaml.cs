using iTextSharp.text.pdf;
using System;
using System.Diagnostics;  // Necesario para abrir el explorador de archivos
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace CompresorPDF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CargarArchivos_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Archivos PDF (*.pdf)|*.pdf";
            openFileDialog.Multiselect = true;
            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    ArchivosListBox.Items.Add(filename);
                }
            }
        }

        private async void ComprimirArchivos_Click(object sender, RoutedEventArgs e)
        {
            if (ArchivosListBox.Items.Count == 0)
            {
                MessageBox.Show("Por favor, cargue uno o más archivos PDF antes de intentar comprimir.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Crear carpeta "compressed" en "Documentos"
            string carpetaSalida = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "compressed");
            if (!Directory.Exists(carpetaSalida))
            {
                Directory.CreateDirectory(carpetaSalida);
            }

            ProgresoCompresion.Value = 0;
            EstadoTextBlock.Text = "Estado: Comenzando la compresión...";

            int totalArchivos = ArchivosListBox.Items.Count;
            int archivosProcesados = 0;

            foreach (string file in ArchivosListBox.Items)
            {
                await ComprimirPDFAsync(file, carpetaSalida);

                archivosProcesados++;
                ProgresoCompresion.Value = (archivosProcesados / (double)totalArchivos) * 100;
            }

            // Abrir el directorio solo una vez cuando todos los archivos hayan sido comprimidos
            AbrirDirectorio(carpetaSalida);

            EstadoTextBlock.Text = "Estado: Compresión completada.";
            MessageBox.Show("Todos los archivos han sido comprimidos.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ComprimirPDFAsync(string file, string carpetaSalida)
        {
            // Crear el archivo de salida en la carpeta "compressed"
            string outputFile = Path.Combine(carpetaSalida, Path.GetFileNameWithoutExtension(file) + "_comprimido.pdf");

            try
            {
                await Task.Run(() =>
                {
                    using (var reader = new PdfReader(file))
                    {
                        using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                        {
                            var document = new iTextSharp.text.Document(reader.GetPageSizeWithRotation(1));
                            var writer = PdfWriter.GetInstance(document, fs);
                            writer.SetFullCompression();
                            document.Open();

                            PdfContentByte cb = writer.DirectContent;

                            for (int i = 1; i <= reader.NumberOfPages; i++)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ProgresoCompresion.Value = (i / (double)reader.NumberOfPages) * 100;
                                    EstadoTextBlock.Text = $"Estado: Comprimiendo {Path.GetFileName(file)} - Página {i}/{reader.NumberOfPages}";
                                });

                                document.SetPageSize(reader.GetPageSizeWithRotation(i));
                                document.NewPage();

                                PdfImportedPage page = writer.GetImportedPage(reader, i);
                                int rotation = reader.GetPageRotation(i);

                                if (rotation == 90 || 270 == rotation)
                                {
                                    cb.AddTemplate(page, 0, -1f, 1f, 0, 0, reader.GetPageSizeWithRotation(i).Height);
                                }
                                else
                                {
                                    cb.AddTemplate(page, 1f, 0, 0, 1f, 0, 0);
                                }
                            }

                            document.Close();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error al comprimir el archivo {file}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void AbrirDirectorio(string carpeta)
        {
            // Abrir el explorador de archivos en la ubicación de la carpeta comprimida
            Process.Start("explorer.exe", carpeta);
        }
    }
}
