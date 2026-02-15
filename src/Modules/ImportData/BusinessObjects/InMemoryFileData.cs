using System;
using System.IO;
using DevExpress.Persistent.Base;

namespace Xpand.XAF.Modules.ImportData.BusinessObjects{
	public class InMemoryFileData : IFileData{
		byte[] _content = Array.Empty<byte>();
		string _fileName;

		public string FileName{
			get => _fileName;
			set => _fileName = value;
		}

		public int Size => _content?.Length ?? 0;

		public byte[] Content => _content;

		/// <summary>
		/// Callback invoked after a file is loaded via the file picker.
		/// Parameters: fileName, content bytes.
		/// </summary>
		internal Action<string, byte[]> FileLoaded{ get; set; }

		public void Clear(){
			_content = Array.Empty<byte>();
			_fileName = null;
		}

		public void LoadFromStream(string fileName, Stream source){
			using var ms = new MemoryStream();
			source.CopyTo(ms);
			_content = ms.ToArray();
			_fileName = fileName;
			FileLoaded?.Invoke(_fileName, _content);
		}

		public void SaveToStream(Stream destination){
			if (_content != null && destination != null)
				destination.Write(_content, 0, _content.Length);
		}
	}
}
