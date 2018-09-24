Ad Unit parameters in settings file

networkName - (string) advertising network name witch is listed in AdNetworkAdapter prefab
impressions - (int) number of impressions
waitingResponseTime - (int) time of waiting response
impressionsInSession - (int) nuber of impressins in session

RandomFetchStrategy:
percentage - (int) show the percentage of probability (range 0-100)

SequenceFetchStrategy:
skipFetchIndex - (int) index fetch when an ad unit skip
replaceableNetwork - (string) if the network with such name wasn't prepared or all ad units disabled then this unit will be show

After update plugins:
- Comment in Chartboost.cs all changes Time.timeScale in method doUnityPause


