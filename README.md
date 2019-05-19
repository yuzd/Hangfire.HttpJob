Hangfire.HttpJob for .netcore
================================

Hangfire.HttpJob for Hangfire

1. add delay background job by [http post] or on dashbord
2. add recurring job by [http post] or on dashbord
3. search job by jobname on dashbord
4. stop or start job on dashbord
5. cron generator on dashbord

Installation
-------------

This library is available as a NuGet Package:

```
Install-Package Hangfire.HttpJob
```

Usage
------

## 

```csharp
	//StartUp.cs
 
	public virtual void ConfigureServices(IServiceCollection services)
	{
		services.AddHangfire(Configuration);//Configuration是下面的方法
	}

	private void Configuration(IGlobalConfiguration globalConfiguration)
	{
		globalConfiguration.UseStorage(
				new MySqlStorage(
					"Server=localhost;Port=3306;Database=hangfire;Uid=root;Pwd=123456;charset=utf8;SslMode=none;Allow User Variables=True",
					new MySqlStorageOptions
					{
						TransactionIsolationLevel = IsolationLevel.ReadCommitted,
						QueuePollInterval = TimeSpan.FromSeconds(15),
						JobExpirationCheckInterval = TimeSpan.FromHours(1),
						CountersAggregateInterval = TimeSpan.FromMinutes(5),
						PrepareSchemaIfNecessary = false,
						DashboardJobListLimit = 50000,
						TransactionTimeout = TimeSpan.FromMinutes(1),
					}))
			.UseConsole()
			.UseHangfireHttpJob();
	}

	public void Configure(IApplicationBuilder app)
	{
		app.UseHangfireServer();
		app.UseHangfireDashboard("/hangfire",new DashboardOptions
		{
			Authorization = new[] { new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
			{
				RequireSsl = false,
				SslRedirect = false,
				LoginCaseSensitive = true,
				Users = new []
				{
					new BasicAuthAuthorizationUser
					{
						Login = "admin",
						PasswordClear =  "test"
					} 
				}

			}) }
		});
	}
```
How to add Hangfire.HttpJob by restful api
================================
1.add backgroundjob

```
url:http://{hangfireserver}/hangfire/httpjob?op=backgroundjob
method:post
data:
{
  "Method": "POST",
  "ContentType": "application/json",
  "Url": "http://XXXXXXX",
  "DelayFromMinutes": 1,
  "Data": "{\"userName\":\"test\"}",
  "Timeout": 5000,
  "BasicUserName": "",// 如果你希望hangfire执行http的时候带basic认证的话 就设置这2个参数
  "BasicPassword": "",
  "JobName": "test_backgroundjob"
}
```

2.add recurringjob

```
url:http://{hangfireserver}/hangfire/httpjob?op=recurringjob
method:post
data:
{
  "Method": "POST",
  "ContentType": "application/json",
  "Url": "http://XXXXXXX",
  "Data": "{\"userName\":\"test\"}",
  "Timeout": 5000,
  "Corn": "0 12 * */2",
  "BasicUserName": "",// 如果你希望hangfire执行http的时候带basic认证的话 就设置这2个参数
  "BasicPassword": "",
  "JobName": "test_recurringjob"
}
```

How to add Hangfire.HttpJob  in Dashbord
================================
![image](https://images4.c-ctrip.com/target/zb0k14000000wk58p27A6.png)
![image](https://images4.c-ctrip.com/target/zb0p14000000wf3q84C46.png)
![image](https://images4.c-ctrip.com/target/zb0114000000wsw9f5E9F.png)
![image](https://images4.c-ctrip.com/target/zb0u14000000wfy2cBA74.png)
![image](https://images4.c-ctrip.com/target/zb0814000000wg66eDEB1.png)
![image](https://images4.c-ctrip.com/target/zb0p14000000wf3yn5CC8.png)
