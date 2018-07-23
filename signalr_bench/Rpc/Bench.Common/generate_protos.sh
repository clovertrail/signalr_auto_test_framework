echo 'this script not work'
#protoc -I./ --csharp_out .  ./Bench.proto --grpc_out . --plugin=protoc-gen-grpc=`which grpc_csharp_plugin`