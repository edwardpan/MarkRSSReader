using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteExample
{
    public class Example
    {
        //// sqlite使用示例，添加了如下内容：
        //// 1、sqlite3.dll(必须在根目录)，
        //// 2、Database文件夹(包含sqlite的.net版操作API，Test.cs为示例表对象)
        //// 3、Microsoft Visual C++ Runtime Package引用(使用该引用必须指定APP运行CPU为x86)
        //var dbPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "test.db");
        //using (var db = new SQLite.SQLiteConnection(dbPath)) {
        //    db.CreateTable<Database.Test>();

        //    db.RunInTransaction(() => {
        //        for (var i = 0; i < 10; i++) {
        //            db.Insert(new Database.Test() { ID = i.ToString(), Name = "Group-" + i.ToString() });
        //        }
        //    });

        //    SQLite.TableQuery<Database.Test> q = db.Table<Database.Test>();
        //    foreach (Database.Test g in q) {
        //        g.Name += "a";
        //    }
        //}
    }
}
