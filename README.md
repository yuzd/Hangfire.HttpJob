Hangfire.HttpJob for .netcore
================================

Hangfire.HttpJob for Hangfire

1. add delay background job by [http post] or on dashbord
2. add recurring job by [http post] or on dashbord
3. search job by jobname on dashbord
4. stop or start job on dashbord
5. cron generator on dashbord
6. use Hangfire.HttpJob.Agent extention to quick develop job program

   6.1 Make your webjob very convenient to support scheduling execution
   
   6.2 Visualizing the execution process of webjob by logs and progress on hangfire dashbord
   
   6.3 Variety of webjob types with different life cycles
   
   	6.3.1 Singleton
	
	6.3.2 Transient 
	
	6.3.3 Hang up until stop command
	

# wiki

00.QickStart

01.how to create backgroud httpjob

02.how to create recurringHttpJob

03.how to use HttpJob.Agent

04.how to use in sqlserver

05.how to config mail service to report job result

https://github.com/yuzd/Hangfire.HttpJob/wiki

Installation
-------------

This library is available as a NuGet Package:

```
Install-Package Hangfire.HttpJob

Install-Package Hangfire.HttpJob.Agent

Install-Package Hangfire.HttpJob.Client
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
# add Hangfire HttpJob by client

``` 
    Install-Package Hangfire.HttpJob.Client

    var serverUrl = "http://localhost:5000/job";
    var result = HangfireJobClient.AddBackgroundJob(serverUrl, new BackgroundJob
    {
	JobName = "测试api",
	Method = "Get",
	Url = "http://localhost:5000/testaaa",
	Mail = new List<string> {"1877682825@qq.com"},
	SendSucMail = true,
	DelayFromMinutes = 1
    }, new HangfireServerPostOption
    {
	BasicUserName = "admin",
	BasicPassword = "test"
    });
    
    var result = HangfireJobClient.AddRecurringJob(serverUrl, new RecurringJob()
    {
	JobName = "测试5点40执行",
	Method = "Post",
	Data = new {name = "aaa",age = 10},
	Url = "http://localhost:5000/testpost",
	Mail = new List<string> { "1877682825@qq.com" },
	SendSucMail = true,
	Cron = "40 17 * * *"
    }, new HangfireServerPostOption
    {
	BasicUserName = "admin",
	BasicPassword = "test"
    });
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
![image](https://images4.c-ctrip.com/target/zb0c14000000wimqtC772.png)
## Email notify
![image](https://images4.c-ctrip.com/target/zb0514000000wihim765F.png)

## Thanks for the Rider IDE provided by JetBrains
[![](https://images4.c-ctrip.com/target/zb021d000001ed9clDFB6.png)](https://www.jetbrains.com/?from=Hangfire.HttpJob)
