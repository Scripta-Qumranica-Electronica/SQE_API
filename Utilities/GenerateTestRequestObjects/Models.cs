using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

// ReSharper disable ArrangeRedundantParentheses

namespace GenerateTestRequestObjects
{
	public class ControllerField
	{
		public ControllerField(string modifiers, string name, string type)
		{
			this.modifiers = modifiers;
			this.name = name;
			this.type = type;
		}

		public string modifiers { get; }
		public string name      { get; }
		public string type      { get; }
	}

	public class FieldComparer : IEqualityComparer<ControllerField>
	{
		public bool Equals(ControllerField x, ControllerField y) => (x != null)
																	&& (y != null)
																	&& x.modifiers.Equals(
																			y.modifiers)
																	&& x.name.Equals(y.name)
																	&& x.type.Equals(y.type);

		public int GetHashCode(ControllerField obj)
		{
			var md5Hasher = MD5.Create();

			var hashed = md5Hasher.ComputeHash(
					Encoding.UTF8.GetBytes(obj.modifiers + obj.name + obj.type));

			return BitConverter.ToInt32(hashed, 0);
		}
	}

	public class ParameterDescription
	{
		public ParameterDescription(string paramType, string paramName)
		{
			ParamType = paramType;
			ParamName = paramName;
		}

		public string ParamType { get; set; }
		public string ParamName { get; set; }
	}

	public class ApiRequestEndpointDescription
	{
		public ApiRequestEndpointDescription()
		{
			routeParams = new List<ParameterDescription>();
			queryParams = new List<ParameterDescription>();
			listeners = new List<ParameterDescription>();
		}

		public HttpMethod                 RequestType { get; set; }
		public string                     IType       { get; set; }
		public string                     OType       { get; set; }
		public string                     HttpString  { get; set; }
		public List<ParameterDescription> routeParams { get; set; }
		public List<ParameterDescription> queryParams { get; set; }
		public List<ParameterDescription> listeners   { get; set; }
		public ParameterDescription       bodyParams  { get; set; }
		public string                     comments    { get; set; }
	}

	public class ApiRequestsDescription
	{
		public ApiRequestsDescription() => requests =
				new Dictionary<HttpMethod, List<ApiRequestEndpointDescription>>();

		public Dictionary<HttpMethod, List<ApiRequestEndpointDescription>> requests { get; set; }
	}

	public class MethodDescription<TSyntax>
			where TSyntax : SyntaxNode
	{
		public MethodDescription() { }

		public MethodDescription(string symbol, TSyntax syntax) : this()
		{
			Symbol = symbol;
			Syntax = syntax;
		}

		public string  Symbol { get; set; }
		public TSyntax Syntax { get; set; }
	}
}
