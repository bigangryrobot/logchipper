logchipper
==========
Due to issues with the log collection script lately and though it is agreeably less worse than the original script, it still has its issues. In an attempt to provide you a more solid product that can scale even further than the last one, I have put together a v12. I call it the “logchipper”. What does the new version provide?

Written in C# dotnet 4.5 : so your developers can support it after I no longer do
Written as a service and console app : run it as a console app to get access to testing features or run it as a service to ensure that its always live
Does not rely on external applications im parsing the logs manually without the need for logparser (surprisingly faster and more stable). Note that your log files MUST have the following header or they will not work with this system:
  `Fields: date time s-sitename s-computername s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username
  c-ip cs-version cs(User-Agent) cs(Cookie) cs(Referer) cs-host sc-status sc-substatus
  sc-win32-status sc-bytes cs-bytes time-taken`
 
It’s REALLY fast : in 13.26 seconds I processed 21 of your production PF2 logs into sql server with 47676 rows created.
Can push data to ANY system : Its coded and tested for SQL imports at this time, BUT I’ve included provisions for both elasticsearch and raw json posting of data. If there is another system, it can be easily added given the framework that I have built.
Appconfig variables everywhere : I’d hate for you to have to recompile to change things (that’s why it was a script before…), so I’ve put allot of flexibility in the app.config
Robust logging : current system is plagued by a poor logging system that I hacked together, this one has a queue, low lockoverhead and other features to ensure that you have the insight you need.
Basic principle of operation:

1. Reads the same csv file that you have been using (yes we can go away from that soon, I left it for now). You can still mark keep as well.
2. Async Foreach server in that csv it will check for the existence of the server and then
3. Async Foreach log in remote directory it will check for locks and chip the log into a data bucket
4. There are 4 data buckets at this time,
  1. one for writing to console that helps with debug
  2. one for writing to a MSSQL db
  3. one for elasticsearch
  4. one for raw json posting

See? Really simple, flexible and solid

I’ve spent the past 2 days so far converting my about 2+ months of previous work into this new version. I’ve still got a bunch of features to drop in like archival, support for smarterstats, automatic thread scaling for sql imports and a few other things before its ready for primetime, though I’m hoping that by Friday ill have something ready for you.

Clark
