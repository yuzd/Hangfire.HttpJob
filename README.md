Hangfire.HttpJob for .netcore
================================

Hangfire.HttpJob for Hangfire


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

	public virtual void ConfigureServices(IServiceCollection services)
	{
		services.AddHangfire(Configuration);
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

![image](https://github.com/yuzd/Hangfire.HttpJob/blob/master/pic1.png)
![image](https://github.com/yuzd/Hangfire.HttpJob/blob/master/pic2.png)
![image](https://github.com/yuzd/Hangfire.HttpJob/blob/master/pic3.png)
![image](https://github.com/yuzd/Hangfire.HttpJob/blob/master/pic4.png)
