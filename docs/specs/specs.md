# Software Requirements Specification (SRS)
## Project: Code Review Tool
**Version:** 1.0.0
**Status:** Draft

---

### 1. Technical Stack Overview
* **Frontend:** Angular 21 (Workspace Architecture)
* **UI Framework:** Angular Material (Dark Theme)
* **Backend:** Microservices Architecture
* **Communication:** API Gateway, Redis Pub/Sub, SignalR, Message Broker (as per docs\specs\message-design.spec.md and docs\specs\subscription-design.spec.md)

### 2. Functional Requirements

#### FR-01: Workspace Structure
**Statement:** The project must be initialized as an Angular Workspace to promote reusability.
* **AC 1.1:** Create a library named `code-review-tools-components`.
* **AC 1.2:** Create an application named `code-review-tools`.
* **AC 1.3:** All Material UI theme configurations (Dark Mode) must be applied at the workspace level or shared via the library.

#### FR-02: Comparison Input Module
**Statement:** The user must be able to define the scope of the code review via the UI.
* **AC 2.1:** Provide text inputs for `Repo Path`, `Target Branch`, and `Source Branch`.
* **AC 2.2:** Implementation of a "Compare" button that initiates the backend workflow.

#### FR-03: Git Extraction Service
**Statement:** The backend must handle file system operations and Git logic.
* **AC 3.1:** Service must navigate to the provided `Repo Path`.
* **AC 3.2:** Execute checkout for both branches to extract file states.
* **AC 3.3:** Filter out any files/directories listed in `.gitignore`.
* **AC 3.4:** Calculate the diff between Source and Target.

#### FR-04: Event-Driven Communication
**Statement:** Data must flow through the system using asynchronous messaging and real-time sockets.
* **AC 4.1:** Processing service must publish an event to Redis Pub/Sub upon diff completion.
* **AC 4.2:** The event message must include: `SourceContent`, `DiffData`, and `Metadata`.
* **AC 4.3:** The SignalR service must bridge the Redis event to the connected Frontend client.

#### FR-05: Review Interface
**Statement:** The UI must display the results in a readable "Review Mode."
* **AC 5.1:** Render side-by-side or unified diff views.
* **AC 5.2:** Enable "Comment" functionality on a per-line basis.
* **AC 5.3:** Comments must be visually anchored to the selected line of code.

---

### 3. Non-Functional Requirements
* **Performance:** The diff for a standard repository (<500 files changed) should be delivered within 3 seconds.
* **Scalability:** The API Gateway must handle concurrent SignalR connections from multiple developers.
* **Usability:** The Dark Theme must comply with WCAG AA contrast standards.
