using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TestAppImport.Module.BusinessObjects {
    [DefaultClassOptions]
    [NavigationItem("Customers")]
    /// <summary>
    /// Represents a customer in the system
    /// </summary>
    public class Customer : BaseObject
    {
        [DisplayName("Name")]
        [Required]
        [StringLength(100)]
        /// <summary>
        /// The customer's full name
        /// </summary>
        public virtual string Name { get; set; }

        [DisplayName("Email")]
        [Required]
        [StringLength(255)]
        [RegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage = "Please enter a valid email address.")]
        /// <summary>
        /// The customer's email address
        /// </summary>
        public virtual string Email { get; set; }

    }
}
