using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.EFCore.DesignTime;
using DevExpress.ExpressApp.EFCore.Updating;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TestAppImport.Module.BusinessObjects {
    [TypesInfoInitializer(typeof(DbContextTypesInfoInitializer<TestAppImportEFCoreDbContext>))]
    public class TestAppImportEFCoreDbContext : DbContext {
        public TestAppImportEFCoreDbContext(DbContextOptions<TestAppImportEFCoreDbContext> options) : base(options) {
        }
        //public DbSet<ModuleInfo> ModulesInfo { get; set; }
        public DbSet<ModelDifference> ModelDifferences { get; set; }
        public DbSet<ModelDifferenceAspect> ModelDifferenceAspects { get; set; }
        public DbSet<PermissionPolicyRole> Roles { get; set; }
        public DbSet<TestAppImport.Module.BusinessObjects.ApplicationUser> Users { get; set; }
        public DbSet<TestAppImport.Module.BusinessObjects.ApplicationUserLoginInfo> UserLoginsInfo { get; set; }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Order> Orders { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseDeferredDeletion(this);
            modelBuilder.UseOptimisticLock();
            modelBuilder.SetOneToManyAssociationDeleteBehavior(DeleteBehavior.SetNull, DeleteBehavior.Cascade);
            modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotificationsWithOriginalValues);
            modelBuilder.UsePropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction);
            modelBuilder.Entity<TestAppImport.Module.BusinessObjects.ApplicationUserLoginInfo>(b => {
                b.HasIndex(nameof(DevExpress.ExpressApp.Security.ISecurityUserLoginInfo.LoginProviderName), nameof(DevExpress.ExpressApp.Security.ISecurityUserLoginInfo.ProviderUserKey)).IsUnique();
            });
            modelBuilder.Entity<ModelDifference>()
                .HasMany(t => t.Aspects)
                .WithOne(t => t.Owner)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
