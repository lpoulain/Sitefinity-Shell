using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Authentication;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.IO;

namespace SitefinitySupport.Shell
{
	public class ShellHttpClient
	{
		protected System.Net.Http.HttpClient client;
		protected string url;
		protected string hostname;
		protected string response;

		public ShellHttpClient(string url, NetworkCredential credentials = null) {
			var handler = new HttpClientHandler { Credentials = credentials };

			HttpClient client = new HttpClient(handler);
			client.BaseAddress = new Uri(url);

			this.url = url;
			this.hostname = url;
		}

		public string getResponse() { return response; }
		public void AddHeader(MediaTypeWithQualityHeaderValue val) {
			client.DefaultRequestHeaders.Accept.Add(val);
		}

		public string Call()
		{
			String message = "";
			String errorMessage = "";

			var handler = new HttpClientHandler();
			handler.AllowAutoRedirect = false;
			String hostName = url;

			try
			{
				Uri uri = new Uri(url);
				hostName = uri.Host;
				HttpClient client = new HttpClient(handler);
				client.BaseAddress = uri;
				HttpResponseMessage response = client.GetAsync(uri.AbsolutePath).Result;  // Blocking call!
				this.response = response.Content.ReadAsStringAsync().Result;
				if (response.IsSuccessStatusCode)
				{
					message = String.Format("Success: {0} bytes (HTTP {1})", response.Content.ReadAsStringAsync().Result.Length, response.StatusCode);
				}
				else
				{
					errorMessage = String.Format("HTTP Response: {0} ({1})", response.StatusCode, (int)response.StatusCode);
				}
			}
			catch (AggregateException ae)
			{
				ae.Handle((ex) =>
				{
					do
					{
						if (ex is WebException)
						{
							WebException wex = ex as WebException;
							switch (wex.Status)
							{
								// The SSL/TLS connection could not be established. 
								case WebExceptionStatus.SecureChannelFailure:
									List<string> protocols = new List<string>();
									if ((ServicePointManager.SecurityProtocol & SecurityProtocolType.Ssl3) != 0) protocols.Add("SSL 3");
									if ((ServicePointManager.SecurityProtocol & SecurityProtocolType.Tls) != 0) protocols.Add("TLS 1.0");
									if ((ServicePointManager.SecurityProtocol & SecurityProtocolType.Tls11) != 0) protocols.Add("TLS 1.1");
									if ((ServicePointManager.SecurityProtocol & SecurityProtocolType.Tls12) != 0) protocols.Add("TLS 1.2");
									errorMessage = String.Format("Could not establish SSL/TLS connection (supported protocols: {0})", String.Join(", ", protocols));
									break;
								case WebExceptionStatus.TrustFailure:
									errorMessage = String.Format("SSL Certificate for '{0}' invalid", hostName);
									break;
								case WebExceptionStatus.NameResolutionFailure:
									errorMessage = String.Format("Invalid domain name '{0}'", hostName);
									break;
								default:
									errorMessage = wex.Status.ToString();
									break;
							}
						}
						else if (ex is SocketException)
						{
							SocketException sex = ex as SocketException;
							errorMessage = sex.Message;
						}
						else if (ex is IOException)
						{
							IOException ioex = ex as IOException;
							errorMessage = ioex.Message;
						}

						ex = ex.InnerException;
					} while (ex != null);
					return true;
				});
			}
			catch (Exception ex)
			{
				errorMessage = ex.Message;
			}

			if (errorMessage != "")
			{
				throw new Exception(errorMessage);
			}
			return message;
		}

	}
}
