using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TestAppImport.Module.BusinessObjects {
    [DefaultClassOptions]
    [NavigationItem("Contacts")]
    /// <summary>
    /// Individual contact associated with an organization
    /// </summary>
    public class Contact : BaseObject
    {
        [DisplayName("Phone")]
        [StringLength(20)]
        /// <summary>
        /// Contact's phone number
        /// </summary>
        public virtual string Phone { get; set; }

        [DisplayName("Last Name")]
        [Required]
        [StringLength(100)]
        /// <summary>
        /// Last name of the contact
        /// </summary>
        public virtual string LastName { get; set; }

        [DisplayName("First Name")]
        [Required]
        [StringLength(100)]
        /// <summary>
        /// First name of the contact
        /// </summary>
        public virtual string FirstName { get; set; }

        [DisplayName("Job Title")]
        [StringLength(100)]
        /// <summary>
        /// Contact's job title
        /// </summary>
        public virtual string JobTitle { get; set; }

        [DisplayName("Organization Id")]
        [Required]
        /// <summary>
        /// Reference to the associated organization
        /// </summary>
        public virtual int OrganizationId { get; set; }

        [DisplayName("Email")]
        [StringLength(255)]
        [RegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage = "Please enter a valid email address.")]
        /// <summary>
        /// Contact's email address
        /// </summary>
        public virtual string Email { get; set; }

    }
}
