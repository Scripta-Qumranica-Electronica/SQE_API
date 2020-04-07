using System;
using System.Data.Common;


namespace qwb_to_sqe
{
    public struct QwbAccesData
    {
        private static String host = "127.0.0.1";
        private static string port = "3306";
        private static String dbUser = "QWB";
        private static String dbPW = "10QWB01";


        public static String dbConnection = $@"
                                                host={host};
                                                port={port};
                                                user={dbUser};
                                                password={dbPW};
                                                database=QWB";
    }
}