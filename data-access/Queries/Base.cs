namespace SQE.API.DATA.Queries
{
	internal static class SetWorkStatus
	{
		public const string GetQuery = @"
INSERT INTO work_status (work_status_message)
SELECT (@WorkStatus)
FROM dual
WHERE NOT EXISTS
  ( SELECT work_status_message
    FROM work_status
    WHERE work_status_message = @WorkStatus
  ) LIMIT 1
";
	}

	internal static class GetWorkStatus
	{
		public const string GetQuery = @"
SELECT work_status_id
FROM work_status
WHERE work_status_message = @WorkStatus
";
	}
}