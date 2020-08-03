using System.Collections.Generic;

namespace SQE.DatabaseAccess.Helpers
{
    /// <summary>
    ///     Helper class which provide with the database tablenames and their joins for the text element tables
    /// </summary>
    public static class TableData
    {
        /// <summary>
        ///     Internal names für the tables
        /// </summary>
        public enum Table
        {
            manuscript,
            text_fragment,
            line,
            sign
        }

        public enum TerminatorType
        {
            Start,
            End
        }

        public static uint BreakAttributeValue = 9;

        // List of the table data - the sequence represents the parent-child relationship
        private static readonly List<SingleTableData> Data = new List<SingleTableData>
        {
            new SingleTableData {Name = "manuscript", HasData = true, Terminators = new uint[] {14, 15}},
            new SingleTableData {Name = "text_fragment", HasData = true, Terminators = new uint[] {12, 13}},
            new SingleTableData {Name = "line", HasData = true, Terminators = new uint[] {10, 11}},
            new SingleTableData {Name = "sign", HasData = false}
        };

        private static readonly Table LastTable = (Table)Data.Count - 1;

        /// <summary>
        ///     Gives the name of the database table
        /// </summary>
        /// <param name="table"></param>
        /// <returns>Name of the database table</returns>
        public static string Name(Table table)
        {
            return Data[(int)table].Name;
        }

        /// <summary>
        ///     Returns the name of the parent database table or null if no parent exists
        /// </summary>
        /// <param name="table"></param>
        /// <returns>Name of parent database table</returns>
        public static string Parent(Table table)
        {
            return table > 0 ? Data[(int)table - 1].Name : "";
        }

        /// <summary>
        ///     Returns the name of the child database table or empty string if no child exists
        /// </summary>
        /// <param name="table"></param>
        /// <returns>Name of parent database table</returns>
        public static string Child(Table table)
        {
            return table < LastTable ? Data[(int)table + 1].Name : "";
        }

        /// <summary>
        ///     True if the table owns a X_data table
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static bool HasData(Table table)
        {
            return Data[(int)table].HasData;
        }


        /// <summary>
        ///     Returns the name of the connecting table between table and its parent
        ///     or null if no parent exists
        /// </summary>
        /// <param name="table"></param>
        /// <returns>Name of connection table with parent</returns>
        public static string ConnectingTableToParent(Table table)
        {
            return table > 0 ? $"{Name(table)}_to_{Parent(table)}" : null;
        }

        public static string TableId(Table table)
        {
            return $"{Name(table)}_id";
        }

        /// <summary>
        ///     Returns the name of the connecting table between table and its child
        ///     or null if no child exists
        /// </summary>
        /// <param name="table"></param>
        /// <returns>Name of connection table with child</returns>
        public static string ConnectingTableToChild(Table table)
        {
            return table < LastTable ? $"{Name(table)}_to_{Child(table)}" : "";
        }


        /// <summary>
        ///     Returns the name of X_data table for the given table or null if it has not data table
        /// </summary>
        /// <param name="table"></param>
        /// <returns>Name of the X_data table</returns>
        public static string DataTableName(Table table)
        {
            return HasData(table) ? $"{Name(table)}_data" : null;
        }

        /// <summary>
        ///     Returns the terminators of the element
        /// </summary>
        /// <param name="table">Element type</param>
        /// <returns>Termoinators as array of uint with start first.</returns>
        public static uint[] Terminators(Table table)
        {
            return Data[(int)table].Terminators;
        }


        /// <summary>
        ///     Returns the start terminator of the element
        /// </summary>
        /// <param name="table">Element type</param>
        /// <returns>Start terminator</returns>
        public static uint StartTerminator(Table table)
        {
            return Terminators(table)[0];
        }

        /// <summary>
        ///     Returns the end terminator of the element
        /// </summary>
        /// <param name="table">Element type</param>
        /// <returns>End terminator</returns>
        public static uint EndTerminator(Table table)
        {
            return Terminators(table)[1];
        }


        /// <summary>
        ///     Returns an array of all terminators of the element and its children depending on terminatorIndex
        /// </summary>
        /// <param name="table">Element type</param>
        /// <param name="terminatorType">
        ///     If start that all start terminators + break is given,
        ///     if end the end terminators
        /// </param>
        /// <returns></returns>
        public static uint[] AllTerminators(Table table, TerminatorType terminatorType)
        {
            // Array receving the terminator ids
            uint[] terminators;
            if (terminatorType == TerminatorType.Start)
            {
                // If Start-Type than we need 
                var count = LastTable - table + 1;
                terminators = new uint[count];
                terminators[count - 1] = BreakAttributeValue;
            }
            else
            {
                terminators = new uint[LastTable - table];
            }

            for (var i = table; i <= LastTable - 1; i++) terminators[i - table] = Terminators(i)[(int)terminatorType];

            return terminators;
        }

        /// <summary>
        ///     Returns the FROM-part of a query which connects the given table with all children down to
        ///     sign attributes and their owners and checks whether the user may read.
        ///     The select part should have "distinct".
        ///     If addDataTables is true also joins to the data tables are set.
        ///     If addPublicEdition is true than the data of public editions would also be found.
        ///     The query contains @UserId and @ElementId as parameters to be set
        /// </summary>
        /// <param name="table">The source table</param>
        /// <param name="addDataTables">If true than als the data tables of the elements are included</param>
        /// <param name="addPublicEdition">If true all public editions will be found too</param>
        /// <returns>Part of query string</returns>
        public static string FromQueryPart(Table table,
            bool addDataTables = false,
            bool addPublicEdition = false,
            bool addRois = false,
            bool addCommentaries = false
        )
        {
            var query = @"
                            FROM sign_interpretation
                                JOIN sign_interpretation_attribute USING (sign_interpretation_id)
                                JOIN sign_interpretation_attribute_owner USING (sign_interpretation_attribute_id)
                                JOIN edition_editor USING (edition_id)
                        ";
            if (addRois)
                query += @"
                                JOIN sign_interpretation_roi USING (sign_interpretation_id)
                                JOIN sign_interpretation_roi_owner
                                    on sign_interpretation_roi_owner.sign_interpretation_roi_id=sign_interpretation
                                JOIN edition_editor USING (edition_id)
                        ";
            if (addPublicEdition)
                query += @"
                                JOIN edition ON edition_editor.edition_id=edition.edition_id
                        ";

            for (var index = LastTable - 1; index >= table; index--)
            {
                query += $@"
                                JOIN {ConnectingTableToChild(index)} USING ({TableId(index + 1)})
                        ";
                if (addDataTables && HasData(index))
                    query += $@"
                                JOIN {DataTableName(index)} USING ({TableId(index)})
                                JOIN {DataTableName(index)}_owner ON 
                                            {DataTableName(index)}.{DataTableName(index)}_id
                                                   = {DataTableName(index)}_owner.{DataTableName(index)}_id
                                        AND {DataTableName(index)}_owner.edition_id=edition_editor.edition_id
                         ";
            }

            query += $@"
                            WHERE {TableId(table)} = @ElementId
                            AND ((edition_editor.user_id = @UserId AND edition_editor.may_read=1)
                        ";
            query += addPublicEdition ? "OR edition.public = 1)" : ")";
            return query;
        }

        public static string GetChildrenIdsQuery(Table table)
        {
            var child = Child(table);
            return $@"SELECT {child}_id
            FROM {ConnectingTableToChild(table)}
            JOIN {child}_data USING ({child}_id)
            JOIN {child}_data_owner USING ({child}_data_id)
            WHERE {Name(table)}_id = @ElementId 
            AND {DataTableName(table)}_owner.edition_id = @EditionId";
        }

        public static string GetDataIdQuery(Table table)
        {
            var dataTable = DataTableName(table);
            return $@"SELECT {dataTable}_id
                    FROM {dataTable}
            JOIN {dataTable}_owner USING ({dataTable}_id)
        WHERE {dataTable}t.{Name(table)}_id = @ElementId
        AND {dataTable}_owner.edition_id= @EditionId;";
        }

        /// <summary>
        ///     Holds the data for a single element table
        /// </summary>
        internal struct SingleTableData
        {
            // Db-name of the table
            public string Name;

            // Has a X_data table
            public bool HasData;
            public uint[] Terminators;
        }
    }
}