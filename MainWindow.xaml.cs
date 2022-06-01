using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Collections;
using System.Windows.Forms;

namespace SourceCodeFilesComplier
{
	public partial class MainWindow : Window
	{
		private SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(255, 220, 220));
		private SolidColorBrush OkBrush = new SolidColorBrush(Color.FromRgb(220, 255, 220));


		/// <exception cref="System.FormatException"/>
		private string[] GetFileExtensionsOrShowErrorToUser()
		{
			var inp = this.FileExtsInputTB.Text.Split(" ,.;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			if (inp.Length <= 0 || inp.Any(x => x.Length > 10) || inp.Any(s => s.Any(c => !char.IsLetterOrDigit(c))))
			{
				this.FileExtsInputTB.Background = this.ErrorBrush;
				throw new FormatException("Unable to get file extensions from user input.");
			}
			else
			{
				this.FileExtsInputTB.Background = this.OkBrush;
				return inp;
			}
		}

		/// <exception cref="System.FormatException"/>
		private DirectoryInfo GetSearchDirectoryOrShowErrorToUser()
		{
			var inp = this.FolderInputTB.Text.Trim();
			var dir = new DirectoryInfo(inp);
			if (!dir.Exists)
			{
				this.FolderInputTB.Background = this.ErrorBrush;
				throw new FormatException("Unable to get search directory from user input.");
			}
			else
			{
				this.FolderInputTB.Background = this.OkBrush;
				this.LastCorrectSearchDirFullName = dir.FullName;
				return dir;
			}
		}

		/// <exception cref="System.AggregateException"/>
		/// <exception cref="System.FormatException"/>
		private IEnumerable<FileInfo> GetInputFilesOrShowErrorToUser()
		{
			var exts = this.GetFileExtensionsOrShowErrorToUser();
			var dir = this.GetSearchDirectoryOrShowErrorToUser();

			var fls = dir.GetFiles(string.Empty, (this.SearchSubDirsCB.IsChecked??false) ?
				SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

			return fls.Where(x => exts.Any(e => x.Extension.EndsWith(e)));
		}

		/// <exception cref="System.AggregateException"/>
		/// <exception cref="System.FormatException"/>
		private IEnumerable<string> GetInputFilesTextsOrShowErrorToUser()
		{
			return GetInputFilesOrShowErrorToUser().Select(x => File.ReadAllText(x.FullName));
		}

		private string LastCorrectSearchDirFullName;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void OpenFolderDialogBT_Click(object sender, RoutedEventArgs e)
		{
			var fd = new FolderBrowserDialog();
			fd.ShowDialog();
			if (!string.IsNullOrEmpty(fd.SelectedPath))
			{
				this.FolderInputTB.Text = fd.SelectedPath;
			}
			fd.Dispose();
		}

		private string GenerateOutput(IEnumerable<FileInfo> fis)
		{
			SourceCodeFileFormatter.FileNameVariant FileNamesCult= SourceCodeFileFormatter.FileNameVariant.FULL_PATH;
			if (this.ShortToSharedFolderRB.IsChecked ?? false) FileNamesCult = SourceCodeFileFormatter.FileNameVariant.TO_SHARED_PATH;
			else if (this.ShortToFileNameRB.IsChecked ?? false) FileNamesCult = SourceCodeFileFormatter.FileNameVariant.FILE_NAME_ONLY;

			SourceCodeFileFormatter proceeder = new SourceCodeFileFormatter
			{
				AddFileNames = this.AddFileNamesCB.IsChecked ?? false,
				UseFileNameVariant = FileNamesCult,
				RemoveLineIndets = this.RemoveIndentCB.IsChecked ?? false,
				RemoveEmptyLines = this.RemoveEmptyCB.IsChecked ?? false,
				AddLinesNumbers = this.AddLineNumsCB.IsChecked ?? false,
				UseCommonLinesNumsLength = this.CommonLinesNumsLengthCB.IsChecked ?? false,
				FilesSharedDirectory = this.LastCorrectSearchDirFullName
			};

			var outputBuilder = new StringBuilder((int)fis.Sum(x => x.Length / 50));

			foreach (var f in fis) { outputBuilder.AppendLine(proceeder.ProceedSourceCode(f));}

			return outputBuilder.ToString();
		}

		private void ProceedTB_Click(object sender, RoutedEventArgs e)
		{
			this.OutputRTB.Document.Blocks.Clear();

			IEnumerable<FileInfo> files;
			try { files = GetInputFilesOrShowErrorToUser(); } catch { return; }

			this.OutputRTB.AppendText(this.GenerateOutput(files));
		}

		private void ExportToFileTB_Click(object sender, RoutedEventArgs e)
		{
			var fd = new SaveFileDialog();
			fd.Filter = @"Text files (*.txt)|*.txt|All files (*.*)|*.*";
			fd.ShowDialog();

			if (!string.IsNullOrEmpty(fd.FileName))
			{
				IEnumerable<FileInfo> files;
				try { files = GetInputFilesOrShowErrorToUser(); } catch { return; }
				File.WriteAllText(fd.FileName, this.GenerateOutput(files));
			}
		}
	}
}