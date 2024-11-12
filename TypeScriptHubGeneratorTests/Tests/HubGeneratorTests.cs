namespace TypeScriptHubGeneratorTests.Tests;

public class Tests
{
    [Test]
    public void CreateFromHub()
    {
        const string expected =
            """
            import type { HubConnection } from "@microsoft/signalr";

            import type { EventType } from "./types/EventType";
            import type { SomeObjectModel } from "./types/SomeObjectModel";

            export class TestHubClient {
              readonly connection: HubConnection;
            
              constructor(hubConnection: HubConnection) {
                this.connection = hubConnection;
              }
            
              async doStuffObject(someObject: SomeObjectModel) {
                await this.connection.invoke<SomeObjectModel>("doStuffObject", someObject);
              }
            
              async doStuffObjectNullable(someObject: SomeObjectModel | null) {
                await this.connection.invoke<SomeObjectModel>("doStuffObjectNullable", someObject);
              }
            
              async doStuffNullableInt(number: number | null, otherNumber: number) {
                await this.connection.invoke<number>("doStuffNullableInt", number, otherNumber);
              }
            
              async doStuffWithEnum(someNullableEnum: EventType | null, someEnum: EventType) {
                await this.connection.invoke<EventType>("doStuffWithEnum", someNullableEnum, someEnum);
              }
            
              addPongHandler(callback: () => void): void {
                this.connection.on("pong", callback);
              }
            
              removePongHandler(callback: () => void): void {
                this.connection.off("pong", callback);
              }
            
              addSomethingHappenedHandler(callback: (message: string | null) => void): void {
                this.connection.on("somethingHappened", callback);
              }
            
              removeSomethingHappenedHandler(callback: (message: string | null) => void): void {
                this.connection.off("somethingHappened", callback);
              }
            
              addSomethingHappenedNullableHandler(callback: (message: string | null) => void): void {
                this.connection.on("somethingHappenedNullable", callback);
              }
            
              removeSomethingHappenedNullableHandler(callback: (message: string | null) => void): void {
                this.connection.off("somethingHappenedNullable", callback);
              }
            
              addSomethingHappenedModelHandler(callback: (someEvent: EventType) => void): void {
                this.connection.on("somethingHappenedModel", callback);
              }
            
              removeSomethingHappenedModelHandler(callback: (someEvent: EventType) => void): void {
                this.connection.off("somethingHappenedModel", callback);
              }
            
              addSomethingHappenedModelListHandler(callback: (someEventsList: EventType[]) => void): void {
                this.connection.on("somethingHappenedModelList", callback);
              }
            
              removeSomethingHappenedModelListHandler(callback: (someEventsList: EventType[]) => void): void {
                this.connection.off("somethingHappenedModelList", callback);
              }
            }

            """;

        const string eventTypeExpected = """export type EventType = "SomeEvent" | "SomeOtherEvent";""";

        const string someObjectModelExpected =
            """
            import type { EventType } from "./EventType";

            export type SomeObjectModel = {
              someBoolean: boolean;
              someNullableBoolean: boolean | null;
              someString: string;
              someNullableString: string | null;
              someInt: number;
              someNullableInt: number | null;
              someDateTime: string;
              someNullableDateTime: string | null;
              someEvent: EventType;
              someStringDictionary: Record<string, string>;
              someEnumDictionary: Record<EventType, string>;
            };
            """;

        var actual = TypescriptHubGenerator.HubGenerator.CreateFromHub(typeof(TestHub));

        Assert.Multiple(() =>
        {
            Assert.That(actual.HubFile, Is.EqualTo(expected));
            Assert.That(actual.TypeFiles, Has.Count.EqualTo(2));
            Assert.That(actual.TypeFiles["EventType"], Is.EqualTo(eventTypeExpected));
            Assert.That(actual.TypeFiles["SomeObjectModel"], Is.EqualTo(someObjectModelExpected));
        });
    }


    [Test]
    public void CreateReactContextHook()
    {
        const string expected =
            """
            import { useContext } from "react";
            import { SomeHubClientContext } from "./SomeHubClientContext";

            export const useSomeHubClient = () => {
              const context = useContext(SomeHubClientContext);
            
              if (context === undefined) {
                throw Error("Context undefined? Forgot a provider somewhere?");
              }
            
              return context;
            };

            """;

        var actual = TypescriptHubGenerator.HubGenerator.CreateReactContextHook("SomeHubClient");

        Assert.That(actual, Is.EqualTo(expected));
    }


    [Test]
    public void CreateReactContext()
    {
        const string expected =
            """
            import { HubConnection, HubConnectionState } from "@microsoft/signalr";
            import { createContext, ReactNode, useEffect, useRef } from "react";
            import { SomeHubClient } from "./SomeHubClient";

            export type SomeHubClientContextProviderProps = {
              children: ReactNode;
              hubConnection: HubConnection | (() => HubConnection);
            };

            export const SomeHubClientContext = createContext<{ hub: SomeHubClient } | undefined>(undefined);

            export const SomeHubClientContextProvider = ({ children, hubConnection }: SomeHubClientContextProviderProps) => {
              const connection = typeof hubConnection === "function" ? hubConnection() : hubConnection;
            
              const someHubClient = useRef(new SomeHubClient(connection));
            
              useEffect(() => {
                if (someHubClient.current.connection.state === HubConnectionState.Disconnected) {
                  someHubClient.current.connection.start().catch((err) => console.error(err));
                }
              }, [someHubClient.current.connection.state]);
            
              return <SomeHubClientContext.Provider value={{ hub: someHubClient.current }}>{children}</SomeHubClientContext.Provider>;
            };

            """;

        var actual = TypescriptHubGenerator.HubGenerator.CreateReactContext("SomeHubClient");

        Assert.That(actual, Is.EqualTo(expected));
    }
}