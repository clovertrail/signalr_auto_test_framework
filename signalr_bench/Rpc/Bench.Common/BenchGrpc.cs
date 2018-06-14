// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: Bench.proto
#pragma warning disable 1591
#region Designer generated code

using System;
using System.Threading;
using System.Threading.Tasks;
using grpc = global::Grpc.Core;

namespace Bench.Common {
  public static partial class RpcService
  {
    static readonly string __ServiceName = "Bench.Common.RpcService";

    static readonly grpc::Marshaller<global::Bench.Common.Empty> __Marshaller_Empty = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Bench.Common.Empty.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Bench.Common.Timestamp> __Marshaller_Timestamp = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Bench.Common.Timestamp.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Bench.Common.Strg> __Marshaller_Strg = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Bench.Common.Strg.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Bench.Common.Stat> __Marshaller_Stat = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Bench.Common.Stat.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Bench.Common.Path> __Marshaller_Path = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Bench.Common.Path.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Bench.Common.Force> __Marshaller_Force = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Bench.Common.Force.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Bench.Common.CounterDict> __Marshaller_CounterDict = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Bench.Common.CounterDict.Parser.ParseFrom);

    static readonly grpc::Method<global::Bench.Common.Empty, global::Bench.Common.Timestamp> __Method_GetTimestamp = new grpc::Method<global::Bench.Common.Empty, global::Bench.Common.Timestamp>(
        grpc::MethodType.Unary,
        __ServiceName,
        "GetTimestamp",
        __Marshaller_Empty,
        __Marshaller_Timestamp);

    static readonly grpc::Method<global::Bench.Common.Empty, global::Bench.Common.Strg> __Method_GetCounterJsonStr = new grpc::Method<global::Bench.Common.Empty, global::Bench.Common.Strg>(
        grpc::MethodType.Unary,
        __ServiceName,
        "GetCounterJsonStr",
        __Marshaller_Empty,
        __Marshaller_Strg);

    static readonly grpc::Method<global::Bench.Common.Empty, global::Bench.Common.Stat> __Method_GetState = new grpc::Method<global::Bench.Common.Empty, global::Bench.Common.Stat>(
        grpc::MethodType.Unary,
        __ServiceName,
        "GetState",
        __Marshaller_Empty,
        __Marshaller_Stat);

    static readonly grpc::Method<global::Bench.Common.Path, global::Bench.Common.Stat> __Method_LoadJobConfig = new grpc::Method<global::Bench.Common.Path, global::Bench.Common.Stat>(
        grpc::MethodType.Unary,
        __ServiceName,
        "LoadJobConfig",
        __Marshaller_Path,
        __Marshaller_Stat);

    static readonly grpc::Method<global::Bench.Common.Empty, global::Bench.Common.Stat> __Method_CreateWorker = new grpc::Method<global::Bench.Common.Empty, global::Bench.Common.Stat>(
        grpc::MethodType.Unary,
        __ServiceName,
        "CreateWorker",
        __Marshaller_Empty,
        __Marshaller_Stat);

    static readonly grpc::Method<global::Bench.Common.Force, global::Bench.Common.CounterDict> __Method_CollectCounters = new grpc::Method<global::Bench.Common.Force, global::Bench.Common.CounterDict>(
        grpc::MethodType.Unary,
        __ServiceName,
        "CollectCounters",
        __Marshaller_Force,
        __Marshaller_CounterDict);

    static readonly grpc::Method<global::Bench.Common.Empty, global::Bench.Common.Stat> __Method_RunJob = new grpc::Method<global::Bench.Common.Empty, global::Bench.Common.Stat>(
        grpc::MethodType.Unary,
        __ServiceName,
        "RunJob",
        __Marshaller_Empty,
        __Marshaller_Stat);

    static readonly grpc::Method<global::Bench.Common.Strg, global::Bench.Common.Stat> __Method_Test = new grpc::Method<global::Bench.Common.Strg, global::Bench.Common.Stat>(
        grpc::MethodType.Unary,
        __ServiceName,
        "Test",
        __Marshaller_Strg,
        __Marshaller_Stat);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Bench.Common.BenchReflection.Descriptor.Services[0]; }
    }

    /// <summary>Base class for server-side implementations of RpcService</summary>
    public abstract partial class RpcServiceBase
    {
      public virtual global::System.Threading.Tasks.Task<global::Bench.Common.Timestamp> GetTimestamp(global::Bench.Common.Empty request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Bench.Common.Strg> GetCounterJsonStr(global::Bench.Common.Empty request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Bench.Common.Stat> GetState(global::Bench.Common.Empty request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Bench.Common.Stat> LoadJobConfig(global::Bench.Common.Path request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Bench.Common.Stat> CreateWorker(global::Bench.Common.Empty request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Bench.Common.CounterDict> CollectCounters(global::Bench.Common.Force request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Bench.Common.Stat> RunJob(global::Bench.Common.Empty request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Bench.Common.Stat> Test(global::Bench.Common.Strg request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

    }

    /// <summary>Client for RpcService</summary>
    public partial class RpcServiceClient : grpc::ClientBase<RpcServiceClient>
    {
      /// <summary>Creates a new client for RpcService</summary>
      /// <param name="channel">The channel to use to make remote calls.</param>
      public RpcServiceClient(grpc::Channel channel) : base(channel)
      {
      }
      /// <summary>Creates a new client for RpcService that uses a custom <c>CallInvoker</c>.</summary>
      /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
      public RpcServiceClient(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
      protected RpcServiceClient() : base()
      {
      }
      /// <summary>Protected constructor to allow creation of configured clients.</summary>
      /// <param name="configuration">The client configuration.</param>
      protected RpcServiceClient(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      public virtual global::Bench.Common.Timestamp GetTimestamp(global::Bench.Common.Empty request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return GetTimestamp(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Bench.Common.Timestamp GetTimestamp(global::Bench.Common.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_GetTimestamp, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Timestamp> GetTimestampAsync(global::Bench.Common.Empty request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return GetTimestampAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Timestamp> GetTimestampAsync(global::Bench.Common.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_GetTimestamp, null, options, request);
      }
      public virtual global::Bench.Common.Strg GetCounterJsonStr(global::Bench.Common.Empty request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return GetCounterJsonStr(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Bench.Common.Strg GetCounterJsonStr(global::Bench.Common.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_GetCounterJsonStr, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Strg> GetCounterJsonStrAsync(global::Bench.Common.Empty request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return GetCounterJsonStrAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Strg> GetCounterJsonStrAsync(global::Bench.Common.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_GetCounterJsonStr, null, options, request);
      }
      public virtual global::Bench.Common.Stat GetState(global::Bench.Common.Empty request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return GetState(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Bench.Common.Stat GetState(global::Bench.Common.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_GetState, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Stat> GetStateAsync(global::Bench.Common.Empty request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return GetStateAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Stat> GetStateAsync(global::Bench.Common.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_GetState, null, options, request);
      }
      public virtual global::Bench.Common.Stat LoadJobConfig(global::Bench.Common.Path request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return LoadJobConfig(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Bench.Common.Stat LoadJobConfig(global::Bench.Common.Path request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_LoadJobConfig, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Stat> LoadJobConfigAsync(global::Bench.Common.Path request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return LoadJobConfigAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Stat> LoadJobConfigAsync(global::Bench.Common.Path request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_LoadJobConfig, null, options, request);
      }
      public virtual global::Bench.Common.Stat CreateWorker(global::Bench.Common.Empty request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return CreateWorker(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Bench.Common.Stat CreateWorker(global::Bench.Common.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_CreateWorker, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Stat> CreateWorkerAsync(global::Bench.Common.Empty request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return CreateWorkerAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Stat> CreateWorkerAsync(global::Bench.Common.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_CreateWorker, null, options, request);
      }
      public virtual global::Bench.Common.CounterDict CollectCounters(global::Bench.Common.Force request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return CollectCounters(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Bench.Common.CounterDict CollectCounters(global::Bench.Common.Force request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_CollectCounters, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.CounterDict> CollectCountersAsync(global::Bench.Common.Force request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return CollectCountersAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.CounterDict> CollectCountersAsync(global::Bench.Common.Force request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_CollectCounters, null, options, request);
      }
      public virtual global::Bench.Common.Stat RunJob(global::Bench.Common.Empty request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return RunJob(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Bench.Common.Stat RunJob(global::Bench.Common.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_RunJob, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Stat> RunJobAsync(global::Bench.Common.Empty request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return RunJobAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Stat> RunJobAsync(global::Bench.Common.Empty request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_RunJob, null, options, request);
      }
      public virtual global::Bench.Common.Stat Test(global::Bench.Common.Strg request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return Test(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Bench.Common.Stat Test(global::Bench.Common.Strg request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_Test, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Stat> TestAsync(global::Bench.Common.Strg request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return TestAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Bench.Common.Stat> TestAsync(global::Bench.Common.Strg request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_Test, null, options, request);
      }
      /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
      protected override RpcServiceClient NewInstance(ClientBaseConfiguration configuration)
      {
        return new RpcServiceClient(configuration);
      }
    }

    /// <summary>Creates service definition that can be registered with a server</summary>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    public static grpc::ServerServiceDefinition BindService(RpcServiceBase serviceImpl)
    {
      return grpc::ServerServiceDefinition.CreateBuilder()
          .AddMethod(__Method_GetTimestamp, serviceImpl.GetTimestamp)
          .AddMethod(__Method_GetCounterJsonStr, serviceImpl.GetCounterJsonStr)
          .AddMethod(__Method_GetState, serviceImpl.GetState)
          .AddMethod(__Method_LoadJobConfig, serviceImpl.LoadJobConfig)
          .AddMethod(__Method_CreateWorker, serviceImpl.CreateWorker)
          .AddMethod(__Method_CollectCounters, serviceImpl.CollectCounters)
          .AddMethod(__Method_RunJob, serviceImpl.RunJob)
          .AddMethod(__Method_Test, serviceImpl.Test).Build();
    }

  }
}
#endregion
