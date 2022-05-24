#!/bin/bash

## Collect all the settings into an associative array (the docker container has Bash ^4.0)
declare -A SETTINGS=(
    ## Database settings
    [MysqlHost]=${MYSQL_HOST}
    [MysqlPort]=${MYSQL_PORT}
    [MysqlUsername]=${MYSQL_USER}
    [MysqlPassword]=${MYSQL_PASSWORD}
    [MysqlDatabase]=${MYSQL_DATABASE}

    ## Email settings
    [MailerEmailAddress]=${MAILER_EMAIL_ADDRESS}
    [MailerEmailUsername]=${MAILER_EMAIL_USERNAME}
    [MailerEmailPassword]=${MAILER_EMAIL_PASSWORD}
    [MailerEmailSmtpUrl]=${MAILER_EMAIL_SMTP_URL}
    [MailerEmailSmtpPort]=${MAILER_EMAIL_SMTP_PORT}
    [MailerEmailSmtpSecurity]=${MAILER_EMAIL_SMTP_SECURITY}

    ## Partner website settings
    [WebsiteHost]=${WEBSITE_HOST}

    ## Secret for API JWT generation
    [Secret]=${SQE_API_SECRET}

    ## Logging levels
    [MinimumLevel]=${API_LOGLEVEL}
    [Microsoft]=${DOTNET_LOGLEVEL}
    [System]=${SYSTEM_LOGLEVEL}
    
    ## Redis SignalR backplane settings
    [UseRedis]=${USE_REDIS}
    [RedisHost]=${REDIS_HOST} 
    [RedisPort]=${REDIS_PORT} 
    [RedisPassword]=${REDIS_PASSWORD}
    
    ## Server protocol support options
    [HttpServer]=${HTTP_SERVER}

	## Github stuff
	[GitURL]=${GIT_URL}
	[GitToken]=${GIT_TOKEN}
)

## Iterate over each setting and update appsettings.json if the environment variable has a value
for K in "${!SETTINGS[@]}"; do
    if [[ ! -z "${SETTINGS[$K]}" ]]; then
        sed -i 's/\"'"${K}"'\":[ ]*\".*\"/\"'"${K}"'\": \"'"${SETTINGS[$K]//\//\\/}"'\"/' appsettings.json
    fi
done

## Execute the next command
exec "$@"
