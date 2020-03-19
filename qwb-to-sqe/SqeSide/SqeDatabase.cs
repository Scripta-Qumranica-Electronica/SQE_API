using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using Dapper;

namespace qwb_to_sqe
{
    public class DataData
    {
        public int DataId;
        public bool Owned = false;
    }
    public static class SqeDatabase
    {
        private static MySqlConnection _conn = new MySqlConnection("server=localhost;user=root" +
                                                                  ";database=SQE;" +
                                                                  "port=33067;" +
                                                                  "password=none");

        public static SqeManuscript GetSqeManuscript(string name)
        {
            var SqeManuscript = _conn.QueryFirstOrDefault<SqeManuscript>(@"
                SELECT manuscript_id as id,
                       edition_editor.edition_id as EditionId,
                       edition_editor_id as EditorId
                    from manuscript_data
                    join manuscript_data_owner using (manuscript_data_id)
                    join edition_editor using (edition_editor_id)
                    where manuscript_data.name=@name
                    and edition_editor.user_id=1", new {name = name});
                
            if (SqeManuscript == null)
            {
                SqeManuscript = new SqeManuscript();
                SqeManuscript.Id = _conn.ExecuteScalar<int>(
                    @"insert into manuscript () values (); 
                               select LAST_INSERT_ID()");
                SqeManuscript.EditionId = _conn.ExecuteScalar<int>(
                    @"insert into edition 
                                    (edition.manuscript_id, locked, copyright_holder, collaborators, public)
                                    values (@manuscriptId,1, 'Reinhard G. Kratz', 'Reinhard G. Kratz, Ingo Kottsieper, Annette Steudel',1);
                               select LAST_INSERT_ID()", 
                    new {manuscriptId = SqeManuscript.Id});
                SqeManuscript.EditorId = _conn.ExecuteScalar<int>(
                    @"insert into edition_editor 
                                    (user_id, edition_id, may_write, may_lock, may_read, is_admin)
                                    values (1,@editionId,0,0,1,0);
                               select LAST_INSERT_ID()", 
                    new {editionId = SqeManuscript.EditionId});
            }

            return SqeManuscript;
        }

        public static int GetSqeFragmentId(string name, int previousFragmentId)
        {
            
            SqeFragment = conn.QueryFirstOrDefault<SqeFragment>(@"
                    select text_fragment_data.text_fragment_id as Id,
                    text_fragment_sequence.position as Position
                    from text_fragment_data
                    join text_fragment_sequence using (text_fragment_id)
                        join text_fragment_data_owner using(text_fragment_data_id)
                        join text_fragment_sequence_owner using (text_fragment_sequence_id)
                        where name = @fragment
                        and text_fragment_sequence_owner.edition_id=@edition_id
                        and text_fragment_data_owner.edition_id=@editionId",
                    new {fragment = Fragment, editionId=EditionId});

                if (SqeFragment == null)
                {
                    SqeFragment=new SqeFragment();
                    if (_previousWord.SqeManuscript.Id == SqeManuscript.Id)
                    {
                        SqeFragment.Position = _previousWord.FragmentPosition + 1;
                        conn.Execute(@"
                            update text_fragment_sequence");
                    }

                    SqeFragment.Id = conn.ExecuteScalar<int>(@"
                    insert into text_fragment () values ();
                    select LAST_INSERT_ID()");}

                    conn.Execute(@"
                        insert into text_fragment_data (text_fragment_id, name)
                        values (@fragmentId, @fragment);
                        insert into text_fragment_data_owner (text_fragment_data_id, edition_editor_id, edition_id) 
                        values (LAST_INSERT_ID(), @editorId, @editionId);
                        insert into text_fragment_sequence (text_fragment_id, position) 
                        values (@fragmentId, @position);
                        insert into SQE.text_fragment_sequence_owner (text_fragment_sequence_id, edition_editor_id, edition_id) 
                        values (LAST_INSERT_ID(), @editorId, @editionId);",
                        new {fragmentId=SqeFragmentId, editorId=EditorId, editionId=EditionId, position=FragmentPosition});
                    
                
            
            return 0;
        }

        private static int _addToOwnedData(string tableName, int editionId, int editorId, object data)
        {

            var fieldNames = _getFieldnames(data);
            var existingData = _findOwnedData(tableName, editionId, fieldNames, data);

            if (existingData == null)
            {
               existingData = new DataData()
               {
                   DataId = _addToData(tableName, fieldNames, data)
               }; 
            }


            if (!existingData.Owned) _addToOwned(tableName,existingData.DataId, editionId,editorId);
            
            
            return existingData.DataId;
        }

        private static void _removeOwnedData()
        {
        }


        private static void _addToOwned(string tableName, int dataId, int editionId, int editorId)
        {
            _conn.Execute($@"
                    INSERT INTO {tableName}_owner ({tableName}_id, edition_id, edition_editor_id)
                    VALUES ({dataId}, {editionId}, {editorId})");
        }
        private static int _addToData(string tableName, string[] fieldNames, object data)
        {

            var query = $@"
                INSERT INTO {tableName}
                ({string.Join(",", fieldNames)}) 
                 VALUES (@{string.Join(",@", fieldNames)};

                SELECT LAST_INSERT_ID()";
                
            return _conn.ExecuteScalar<int>(query, data);
        }

        private static string[] _getFieldnames(object data)
        {
            var parameters = data as IEnumerable<KeyValuePair<string, object>>;
            var fieldNames = new string[parameters.Count()];
            var i = 0;
            foreach (var pair in parameters)
            {
                fieldNames[i++] = pair.Key;
            }

            return fieldNames;
        }

        private static DataData _findOwnedData(string tableName, int editionId,  IReadOnlyList<string> fieldNames, object data)
        {
            var searchString = new StringBuilder($@"
                SELECT {tableName}_id AS DataId,
                        IFNULL(edition_id={editionId},0) AS Owned,
                FROM {tableName}
                    LEFT JOIN {tableName}_owner USING ({tableName}_id)
                WHERE {fieldNames[0]} = @{fieldNames[0]}");
            for (var i=1; i<fieldNames.Count; i++)
            {
                searchString.Append($" AND {fieldNames[i]} = @{fieldNames[i]}");
            }

            searchString.Append(" ORDER BY EditionId");

            return _conn.QueryFirstOrDefault<DataData>(searchString.ToString(), data);
        }
    }
}