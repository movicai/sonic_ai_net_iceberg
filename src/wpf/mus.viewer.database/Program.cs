using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mus.viewer.database
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var db = new MyContext())
            {
                // Create
                //var entity = new MyEntity { Name = "Test" };
                //db.MyEntities.Add(entity);
                var device = new tb_device() { name = "test", ipaddress = "192.168.0.1", subnet = "255.255.255.0" };
                db.Tb_Devices.Add(device);
                db.SaveChanges();

                // Read
                var readEntity = db.MyEntities.FirstOrDefault();

                if (readEntity != null)
                {
                    Console.WriteLine("Name: " + readEntity.Name);

                    // Update
                    readEntity.Name = "Updated Test";
                    db.SaveChanges();

                    // Delete
                    db.MyEntities.Remove(readEntity);
                    db.SaveChanges();
                }
            }

        }
    }
}
