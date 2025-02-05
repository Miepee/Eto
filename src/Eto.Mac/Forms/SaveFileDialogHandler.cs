namespace Eto.Mac.Forms
{
	public class SaveFileDialogHandler : MacFileDialog<NSSavePanel, SaveFileDialog>, SaveFileDialog.IHandler
	{
		bool hasShown;
		string selectedFileName;


		public override string FileName
		{
			get => hasShown ? base.FileName : selectedFileName;
			set
			{
				selectedFileName = value;
				var name = value;
				if (!string.IsNullOrEmpty(name))
				{
					SetAllowedFileTypes();
					var dir = Path.GetDirectoryName(name);
					if (!string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir))
						Directory = new Uri(dir);
					name = Path.GetFileName(name);
				}
				Control.NameFieldStringValue = name ?? string.Empty;
				hasShown = false;
			}
		}

		protected override NSSavePanel CreateControl()
		{
			return NSSavePanel.SavePanel;
		}

		protected override void Initialize()
		{
			Control.ExtensionHidden = false;
			Control.AllowsOtherFileTypes = true;
			Control.CanSelectHiddenExtension = true;
			base.Initialize();
		}

		public override DialogResult ShowDialog(Window parent)
		{
			var result = base.ShowDialog(parent);
			if (result == DialogResult.Ok)
			{
				selectedFileName = null;
				hasShown = true;
			}

			return result;
		}

		protected override void OnFileTypeChanged()
		{
			base.OnFileTypeChanged();
			var extension = Widget.CurrentFilter?.Extensions?.FirstOrDefault()?.TrimStart('*', '.');
			if (!string.IsNullOrEmpty(extension))
			{
				var fileName = Control.NameFieldStringValue;
				if (fileName != null)
					Control.NameFieldStringValue = $"{Path.GetFileNameWithoutExtension(fileName)}.{extension}";
			}			
		}
	}
}
