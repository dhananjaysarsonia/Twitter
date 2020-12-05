COP5615 - Fall 2020
December 4, 2020

#### Team Members

##### Forum Gala          UFID: 6635-6557

##### Dhananjay Sarsonia  UFID: 1927-5958


#### How to run
Using terminal, go to the directory containing the ________.fsx file.
- Please make sure dotnet core is installed
- First Run the command: 

dotnet fsi --langversion:preview Server.fsx

- Wait for server to startup 
- Then start the simulator by the following command 

dotnet fsi --langversion:preview Simulator.fsx

- Simulator will ask for number of users. 

NOTE: PLEASE DO NOT PRESS ANY KEY AS IT WILL TERMINATE THE PROGRAM

#### What is working?
We have tested the program for 2500 users. 


#### Known issues
- Above 5000 it seems like Akka.remote is not working properly as we are getting TCP data exception when user feeds are transferred through messages. We are guessing breaking payload will solve this issue and we can scale but Websharper with TCP should solve this issue easily in the next project.


##### Maximum Number of users simulated - 
5000 users are working stable. We can push it to 10000 but as we reported above Akka.remote is generating some errors with data trasnfers.
