using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TestAppImport.Module.BusinessObjects {
    [DefaultClassOptions]
    [NavigationItem("Orders")]
    /// <summary>
    /// Represents a customer order
    /// </summary>
    public class Order : BaseObject
    {
        [DisplayName("Order Date")]
        [Required]
        /// <summary>
        /// The date when the order was placed
        /// </summary>
        public virtual DateTime OrderDate { get; set; }

        [DisplayName("Total Amount")]
        [Required]
        /// <summary>
        /// The total amount of the order
        /// </summary>
        public virtual decimal TotalAmount { get; set; }

    }
}
