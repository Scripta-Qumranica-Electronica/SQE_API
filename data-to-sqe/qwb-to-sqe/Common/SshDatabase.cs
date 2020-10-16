using System.IO;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace qwb_to_sqe.Common
{
	public class SshDatabase
	{
		private readonly IConfiguration     _configuration;
		private          ForwardedPortLocal _port;
		private          SshClient          _sshClient;

		public SshDatabase(string name)
		{
			_configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
													   .AddJsonFile(
															   $"appsettings{name}.json"
															   , false)
													   .Build();

			if (bool.Parse(_configuration["UseSsh"]))
				_createTunnel();
			else
			{
				_port = new ForwardedPortLocal(
						_configuration["ServerUrl"]
						, _configuration.GetValue<uint>("MysqlPort")
						, "127.0.0.1"
						, 0);
			}

			_connectToDatabase();
		}

		public MySqlConnection connection { get; private set; }

		private void _createTunnel()
		{
			if (_sshClient != null)
				return;

			_sshClient = new SshClient(
					_configuration["ServerUrl"]
					, _configuration["ServerUserName"]
					, _configuration["ServerPassword"]);

			_sshClient.Connect();

			if (_sshClient.IsConnected)
			{
				_port = new ForwardedPortLocal("127.0.0.1", "127.0.0.1", 3306);

				_sshClient.AddForwardedPort(_port);
				_port.Start();

				if (!_port.IsStarted)
					throw new SshException("Could not establish port");
			}
			else
				throw new SshException("Could not connect to Server.");
		}

		private void _connectToDatabase()
		{
			var cString = $"server={_port.BoundHost};"
						  + $"port={_port.BoundPort};"
						  + $"database={_configuration["DbTable"]};"
						  + $"username={_configuration["DbUserName"]};"
						  + $"password={_configuration["DbPassword"]}";

			connection = new MySqlConnection(cString);
			connection.Open();
		}
	}
}
