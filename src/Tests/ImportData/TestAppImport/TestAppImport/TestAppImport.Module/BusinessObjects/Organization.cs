using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TestAppImport.Module.BusinessObjects {
    [DefaultClassOptions]
    [NavigationItem("Organizations")]
    /// <summary>
    /// Represents a business or company in the CRM system
    /// </summary>
    public class Organization : BaseObject
    {
        [DisplayName("Name")]
        [Required]
        [StringLength(100)]
        /// <summary>
        /// Name of the organization
        /// </summary>
        public virtual string Name { get; set; }

        [DisplayName("Address")]
        [StringLength(500)]
        /// <summary>
        /// Physical address of the organization
        /// </summary>
        public virtual string Address { get; set; }

        [DisplayName("Email")]
        [StringLength(255)]
        [RegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage = "Please enter a valid email address.")]
        /// <summary>
        /// Contact email address
        /// </summary>
        public virtual string Email { get; set; }

        [DisplayName("Phone")]
        [StringLength(20)]
        /// <summary>
        /// Contact phone number
        /// </summary>
        public virtual string Phone { get; set; }

        [DisplayName("Website")]
        [StringLength(100)]
        /// <summary>
        /// Organization's website URL
        /// </summary>
        public virtual string Website { get; set; }

        [DisplayName("Industry")]
        [StringLength(100)]
        /// <summary>
        /// Industry the organization operates in
        /// </summary>
        public virtual string Industry { get; set; }

    }
}
