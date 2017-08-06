# Lykke.Ninja
=======
# lykke.ninja
The service is used for:
 * retrieving address information from blockchain
 
# Deploy

Put .env file along with the docker-compose.yml
The .env file must define the following environment variables: - SETTINGSURL containing URL that the service must use to acquire settings - LISTENPORT containing port number that the container must expose

# Settings description


```
{
	"NinjaUrl":"", // url for nbitcoin ninja. For more info check this repo: https://github.com/MetacoSA/QBitNinja .
	"Network":"", // blockchain network type (mainnet, testnet, regtest)
	"Db": { //azure table storage connection strings
		"LogsConnString":"", // for logging purposes
		"DataConnString":"",
	},
	
	
	"NinjaData": { // connection to common mongodb db
		"ConnectionString": "",
		"DbName": "" 	
	}
}

```