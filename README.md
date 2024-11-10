# TypeScriptHubGenerator

Create TypeScript clients from SignalR hubs.

For React, a context and hook is created.

# Todo

- Package as a dotnet tool
- Proper cli arguments for configuring stuff
- React context and hooks should be optional

# Using the tool

WIP :D

# Using the client

## React

Using the client requires installing @microsoft/signalr (tested with version 8.0.7)

```typescript jsx
// Configure hub in eg App.tsx
function App() {
    const hubConnection = new HubConnectionBuilder()
        .withAutomaticReconnect()
        .withUrl("someurl")
        .build();

    return (
        <SomeHubClientContextProvider hubConnection={hubConnection}>
            <>...</>
        </SomeHubClientContextProvider>
    );
}
```
```typescript jsx
// Use in some component
export const SomeComponent = () => {
    const someHub = useSomeHubClient();

    const handleSomethingHappened = useCallback((message: string | null) => {
        console.debug("hub: " + message);
    }, []);

    useEffect(() => {
        someHub.hub.addSomethingHappenedHandler(handleSomethingHappened);

        return () => {
            someHub.hub.removeSomethingHappenedHandler(handleSomethingHappened);
        };
    }, [handleSomethingHappened, someHub.hub]);

    return (
        <>...</>
    );
};
```
