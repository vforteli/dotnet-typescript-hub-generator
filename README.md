# TypeScriptHubGenerator

Create TypeScript clients from SignalR hubs.

For React, a context and hook is created.

# Using the tool

```
# install globally (or locally)
dotnet tool install --global vforteli.TypeScriptHubGenerator

# run the tool
dotnet tshubgen \
    --assembly-path "some/folder/assembly.dll" \
    --output-folder "some/other/folder" \
     --create-react-context
```

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

  // Wrap in callback to be able to remove handler in useEffect
  const handleSomethingHappened = useCallback((message: string | null) => {
    console.debug("hub: " + message);
  }, []);

  // Add handler for a callback. The handler must be removed explicitly or duplicates will be added on re-renders
  useEffect(() => {
    someHub.hub.addSomethingHappenedHandler(handleSomethingHappened);

    return () => {
      someHub.hub.removeSomethingHappenedHandler(handleSomethingHappened);
    };
  }, [handleSomethingHappened, someHub.hub]);

  // Invoke a hub method
  const doSomething = () => {
    someHub.hub.doSomething({ somePayload: "hello from component!" });
  };

  return <>...</>;
};
```
