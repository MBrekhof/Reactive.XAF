using System;
using System.Diagnostics.CodeAnalysis;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.ImportData.Tests.BOModel{
	[FriendlyKeyProperty(nameof(Code))]
	[DefaultClassOptions]
	[SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
	public class ImportTestBO : CustomBaseObject{
		public ImportTestBO(Session session) : base(session){ }

		string _code;
		public string Code{
			get => _code;
			set => SetPropertyValue(nameof(Code), ref _code, value);
		}

		string _name;
		public string Name{
			get => _name;
			set => SetPropertyValue(nameof(Name), ref _name, value);
		}

		int _quantity;
		public int Quantity{
			get => _quantity;
			set => SetPropertyValue(nameof(Quantity), ref _quantity, value);
		}

		double _price;
		public double Price{
			get => _price;
			set => SetPropertyValue(nameof(Price), ref _price, value);
		}

		bool _isActive;
		public bool IsActive{
			get => _isActive;
			set => SetPropertyValue(nameof(IsActive), ref _isActive, value);
		}

		DateTime _createdDate;
		public DateTime CreatedDate{
			get => _createdDate;
			set => SetPropertyValue(nameof(CreatedDate), ref _createdDate, value);
		}

		Guid _externalId;
		public Guid ExternalId{
			get => _externalId;
			set => SetPropertyValue(nameof(ExternalId), ref _externalId, value);
		}
	}
}
