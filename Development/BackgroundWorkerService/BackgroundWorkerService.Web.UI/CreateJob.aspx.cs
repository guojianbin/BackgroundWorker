﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services;
using WebUI.BackgroundWorkerService.Service;
using WebUI.datamodel;
using System.Globalization;

namespace WebUI
{
	public partial class CreateJob : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack)
			{
				uniqueId.Text = Guid.NewGuid().ToString();
			}
		}

		[WebMethod]
		public static string Create(string uniqueId, string name, string description, string data, string metaData, string jobType, string absoluteTimeout, string queueId, string application, string group, bool suppressHistory, bool deleteWhenDone, List<KeyValuePairStringString> additionalData, SimpleSchedule schedule)
		{
			try
			{
				TimeSpan? absoluteTimeoutValue = !string.IsNullOrEmpty(absoluteTimeout) ? (TimeSpan?)TimeSpan.Parse(absoluteTimeout) : null;
				byte queueIdValue = !string.IsNullOrEmpty(queueId) ? byte.Parse(queueId) : (byte)0;
				Guid? uniqueIdValue = Helpers.TryParseNullableGuid(uniqueId);

				switch (jobType)
				{
					case "BackgroundWorkerService.Jobs.BasicHttpSoapCallbackJob, BackgroundWorkerService.Jobs":
						string callbackUrl = additionalData.First(d => d.Key == "callbackUrl").Value;
						string contractType = additionalData.First(d => d.Key == "contractType").Value;
						string securityMode = additionalData.First(d => d.Key == "securityMode").Value;
						string credentialType = additionalData.First(d => d.Key == "credentialType").Value;
						string domain = additionalData.First(d => d.Key == "domain").Value;
						string username = additionalData.First(d => d.Key == "username").Value;
						string password = additionalData.First(d => d.Key == "password").Value;
						string methodName = additionalData.First(d => d.Key == "methodName").Value;
						bool ignoreCertificateErrors = bool.Parse(additionalData.First(d => d.Key == "ignoreCertificateErrors").Value);

						try
						{
							Uri uri = new Uri(callbackUrl);
						}
						catch
						{
							throw new ArgumentException("Callback Url specified was not a valid Uri.");
						}

						if (contractType == "Basic")
						{
							metaData =
								global::BackgroundWorkerService.Jobs.JobBuilder.CreateBasicHttpSoap_BasicCallbackJobMetaData(
										metaData,
										callbackUrl,
										(System.ServiceModel.BasicHttpSecurityMode)Enum.Parse(typeof(System.ServiceModel.BasicHttpSecurityMode), securityMode),
										(System.ServiceModel.HttpClientCredentialType)Enum.Parse(typeof(System.ServiceModel.HttpClientCredentialType), credentialType),
										null,
										domain,
										username,
										password,
										ignoreCertificateErrors).Serialize();
						}
						else
						{
							if (string.IsNullOrEmpty(methodName))
								throw new ArgumentException("Method Name must be specified for Composite contracts.");
							metaData =
								global::BackgroundWorkerService.Jobs.JobBuilder.CreateBasicHttpSoap_CompositeBasicCallbackJobMetaData(
										metaData,
										methodName,
										callbackUrl,
										(System.ServiceModel.BasicHttpSecurityMode)Enum.Parse(typeof(System.ServiceModel.BasicHttpSecurityMode), securityMode),
										(System.ServiceModel.HttpClientCredentialType)Enum.Parse(typeof(System.ServiceModel.HttpClientCredentialType), credentialType),
										null,
										domain,
										username,
										password,
										ignoreCertificateErrors).Serialize();
						}
						break;
					case "BackgroundWorkerService.Jobs.SendMailJob, BackgroundWorkerService.Jobs":
						string sendFrom = additionalData.First(d => d.Key == "sendFrom").Value;
						string sendTo = additionalData.First(d => d.Key == "sendTo").Value;
						string sendSubject = additionalData.First(d => d.Key == "sendSubject").Value;
						string emailBody = additionalData.First(d => d.Key == "emailBody").Value;

						using (System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage(sendFrom, sendTo, sendSubject, emailBody))
						{
							global::BackgroundWorkerService.Jobs.JobBuilder.GetSendMailJobDataAndMetaData(message, null, out data, out metaData);
						}
						break;
				}

				CreateNewJob(uniqueIdValue, name, description, data, metaData, jobType, absoluteTimeoutValue, queueIdValue, application, group, suppressHistory, deleteWhenDone, schedule);
			}
			catch (Exception ex)
			{
				return ex.Message;
			}
			return string.Empty;
		}

		private static void CreateNewJob(Guid? uniqueId, string name, string description, string data, string metaData, string jobType, TimeSpan? absoluteTimeout, byte queueId, string application, string group, bool suppressHistory, bool deleteWhenDone, SimpleSchedule schedule)
		{
			CreateJobRequest request = new CreateJobRequest
			{
				UniqueId = uniqueId,
				Application = application,
				DeleteWhenDone = deleteWhenDone,
				Description = description,
				Name = name,
				QueueId = queueId,
				Type = jobType,
				MetaData = metaData,
				Data = data,
				SuppressHistory = suppressHistory,
				AbsoluteTimeout = absoluteTimeout,
				Group = group,
				Status = JobStatus.Pending,
			};

			if (schedule != null)
			{
				if (!schedule.StartDailyAt.HasValue) schedule.StartDailyAt = new TimeSpan();
				request.CalendarSchedule = new CalendarSchedule
				{
					ScheduleType = typeof(global::BackgroundWorkerService.Logic.DataModel.Scheduling.CalendarSchedule).AssemblyQualifiedName,
					DaysOfWeek = schedule.DaysOfWeek.ToArray(),
					StartDailyAt = new TimeOfDay { Hour = schedule.StartDailyAt.Value.Hours, Minute = schedule.StartDailyAt.Value.Minutes, Second = schedule.StartDailyAt.Value.Seconds },
					RepeatInterval = schedule.RepeatInterval,
					EndDateTime = null,
					StartDateTime = !string.IsNullOrEmpty(schedule.StartDateTime) ? DateTime.ParseExact(schedule.StartDateTime, "dd-MM-yyyy HH:mm:ss", CultureInfo.CurrentCulture) : DateTime.Now,
				};
			}

			using (AccessPointClient client = new AccessPointClient())
			{
				client.CreateJob(request);
			}
		}
	}
}