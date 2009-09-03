using System;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace Rhino.DivanDB.Storage
{
    [CLSCompliant(false)]
    public class SchemaCreator
    {
        private readonly Session session;
        public const string SchemaVersion = "1.0";

        public SchemaCreator(Session session)
        {
            this.session = session;
        }

        public void Create(string database)
        {
            JET_DBID dbid;
            Api.JetCreateDatabase(session, database, null, out dbid, CreateDatabaseGrbit.None);
            try
            {
                using (var tx = new Transaction(session))
                {
                    CreateDetailsTable(dbid);
                    CreateDocumentsTable(dbid);
                   
                    tx.Commit(CommitTransactionGrbit.None);
                }
            }
            finally
            {
                Api.JetCloseDatabase(session, dbid, CloseDatabaseGrbit.None);
            }
        }

        private void CreateDocumentsTable(JET_DBID dbid)
        {
            JET_TABLEID tableid;
            Api.JetCreateTable(session, dbid, "documents", 16, 100, out tableid);
            JET_COLUMNID columnid;

            Api.JetAddColumn(session, tableid, "id", new JET_COLUMNDEF
            {
                coltyp = JET_coltyp.Long,
                grbit = ColumndefGrbit.ColumnFixed | ColumndefGrbit.ColumnNotNULL|ColumndefGrbit.ColumnAutoincrement
            }, null, 0, out columnid);

            Api.JetAddColumn(session, tableid, "key", new JET_COLUMNDEF
            {
                cbMax = 255,
                coltyp = JET_coltyp.Text,
                cp = JET_CP.Unicode,
                grbit = ColumndefGrbit.ColumnNotNULL
            }, null, 0, out columnid);

            Api.JetAddColumn(session, tableid, "data", new JET_COLUMNDEF
            {
                coltyp = JET_coltyp.LongText,
                grbit = ColumndefGrbit.ColumnNotNULL
            }, null, 0, out columnid);

            var indexDef = "+id\0\0";
            Api.JetCreateIndex(session, tableid, "pk", CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length,
                               100);

            indexDef = "+key\0\0";
            Api.JetCreateIndex(session, tableid, "by_key", CreateIndexGrbit.IndexDisallowNull | CreateIndexGrbit.IndexUnique, indexDef, indexDef.Length,
                               100);
        }

        private void CreateDetailsTable(JET_DBID dbid)
        {
            JET_TABLEID tableid;
            Api.JetCreateTable(session, dbid, "details", 16, 100, out tableid);
            JET_COLUMNID id;
            Api.JetAddColumn(session, tableid, "id", new JET_COLUMNDEF
            {
                cbMax = 16,
                coltyp = JET_coltyp.Binary,
                grbit = ColumndefGrbit.ColumnNotNULL | ColumndefGrbit.ColumnFixed
            }, null, 0, out id);

            JET_COLUMNID schemaVersion;
            Api.JetAddColumn(session, tableid, "schema_version", new JET_COLUMNDEF
            {
                cbMax = Encoding.Unicode.GetByteCount(SchemaVersion),
                cp = JET_CP.Unicode,
                coltyp = JET_coltyp.Text,
                grbit = ColumndefGrbit.ColumnNotNULL | ColumndefGrbit.ColumnFixed
            }, null, 0, out schemaVersion);


            using (var update = new Update(session, tableid, JET_prep.Insert))
            {
                Api.SetColumn(session, tableid, id, Guid.NewGuid().ToByteArray());
                Api.SetColumn(session, tableid, schemaVersion, SchemaVersion, Encoding.Unicode);
                update.Save();
            }
        }
    }
}