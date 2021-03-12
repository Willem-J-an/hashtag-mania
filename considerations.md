# Deployment
Deployment should be straight forward with the included build steps and scripts. The solution can be deployed in any environment that supports docker-compose and has network connectivity to twitter, such as:
- Any local, private or public cloud virtual machine
- Cloud container platforms such as:
    - Azure Container Instances
    - Azure Web Apps for Containers
With minor modifications it should also be able to run in Kubernetes.

# Change management
There are a number of factors facilitating easy development of the solution. The solution is set up in a modular way. This should make it easy to add or modify functionality. Additionally this makes it possible to write automated solution component tests. The included test and build scripts allow for fast iterations and developer feedback.

Changes to the existing persisted data are a little less straight forward. The current solution only saves the extracted hashtags, rather than all the streamed tweets. If all streamed tweets would be persisted to a storage layer, we would be able to replay the processing of tweets with new logic, and store the new results for analysis. This could be an improvement for the solution.

# Scaling
Scaling is dependent on a few factors:
- Server (twitter)
- Client (this application)
- Database (Postgres)

## Server
The biggest bottleneck with regard to scaling is in the rate limits implemented by Twitter. With the standard app tier, only 500k tweets can be pulled per month. Additionally, there are limits with how many connections the app identities are able to open. To allow us to scale this solution, we would need to investigate how to deal with the rate limits, e.g. by moving from Standard to Premium or Enterprise tier, or/and creating multiple apps identities.

## Client
The containerized client can be deployment mulitiple times to facilitate scaling-out. We need some way to divide/partition the workload accross the various instances. A pragmatic approach would be to have different Twitter Stream Rules for each instance. This should enable each container instance to process a subset of relevant tweets.

## Database
Each client currently comes with its own database. This makes scaling easy, as there are no conflicts when multiple applications try to write and update the same table/row at the same time. A downside to each aplication having its own database is that there is no direct view into the combined results. This can be mitigated by running some compaction job ever hour to combine all persisted hashtag data from the past hour accross all databases into one location.
An alternative option regarding storage would be to stream data into a messaging platform such as Kafka or Azure Event Hubs. This could help to build more real time analysis on the data. It also would be a logical point to bring the data from a scaled out streaming solution back to a single point where insights can be gained.

# Batch vs Streaming
The current solution streams tweets into a database. The benefit of streaming is that data can be processed as it comes in. In general, but especially in the context of Twitter, content is most relevant when it is posted. This solution to find happy/hot hashtags could be used for example to surprise and engage with customers by posting relevant/current content.
