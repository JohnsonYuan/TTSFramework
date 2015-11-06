@rem
@rem Batch file to config distribute computing
@rem
@rem 2005-Dec-08 zweng
@rem 

@rem enable firewall notification mode, let user get blocking information and then take action
netsh firewall set Notification enable

@rem IP location of aggregator, who will coordinate the whole distribute computing resources
set AggregatorIp=172.27.136.50
set AggregatorPort=9922

@rem the listening port of server, who will organize the jobs
set ServerPort=9966

@rem the listening port of client, who will serve as computing resource
set ClientPort=9988