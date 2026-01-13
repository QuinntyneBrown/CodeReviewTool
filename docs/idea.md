Suppose you are a Senior Product Owner

Create specifications for the following tool

Angular Workspace using Angular 21, dark theme and Angular Material UI Components for all frontend components

Workspace has a code-review-tools-components library

Workspace has a code-review-tools application


code-review-tools has a UI where user can 

- enter the file path where a git repo is location
- enter a target branch
- enter a source branch
- press compare

On pressing compare a command goes to the backend

backend is implemented using microservices so the command goes to CodeReviewTool.ApiGateway

A microservice on the backend receives a message as there is a Message Broker as per the messaging specs in docs\messaging

A backend microservice checkouts the main branch of the git repo using the file path provided

loads the contents of the git repo, respecting the git ignore files and not loading anything that is in the git ignore 

checkouts the target branch of the git repo using the file path provided

loads the contents of the git repo, respecting the git ignore files and not loading anything that is in the git ignore

compares the difference between the target branch and  source branch

creates an Event Message with a propery containing the source of the source branch and a property with the diff

the microservice publishes the event on Redis Pub Sub

Another Microservice listends for the event and then publishes the event over WebSockets using SignalR

The frontend is connected the backend using SignalR via the api gateway

the frontend receives events and updates the UI with the source code and the diff allowing the user to review the code and changes and make comments on specific lines

