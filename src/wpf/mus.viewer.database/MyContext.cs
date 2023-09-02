using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mus.viewer.database
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using MySql.Data.MySqlClient;
    using MySql.Data.EntityFramework;
    using System.ComponentModel.DataAnnotations.Schema;

    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class MyContext : DbContext
    {
        public MyContext() : base("server=127.0.0.1;database=mus_viewer_db;uid=root;pwd=1212")
        {
        }

        public DbSet<MyEntity> MyEntities { get; set; }
        public DbSet<tb_device> Tb_Devices { get; set; }
    }

    [Table("myentity")]

    public class MyEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Table("tb_device")]
    public class tb_device
    {
        public int seq { get; set; }
        public string name { get; set; }
        public string ipaddress { get; set; }
        public string subnet { get; set; }
    }

    //class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        using (var db = new MyContext())
    //        {
    //            // Create
    //            var entity = new MyEntity { Name = "Test" };
    //            db.MyEntities.Add(entity);
    //            db.SaveChanges();

    //            // Read
    //            var readEntity = db.MyEntities.FirstOrDefault();

    //            if (readEntity != null)
    //            {
    //                Console.WriteLine("Name: " + readEntity.Name);

    //                // Update
    //                readEntity.Name = "Updated Test";
    //                db.SaveChanges();

    //                // Delete
    //                db.MyEntities.Remove(readEntity);
    //                db.SaveChanges();
    //            }
    //        }
    //    }
    //}

}
