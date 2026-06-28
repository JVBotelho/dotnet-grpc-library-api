# Helper function to generate protobuf and grpc files
function(generate_protobuf_and_grpc proto_file proto_dir out_srcs out_hdrs out_grpc_srcs out_grpc_hdrs)
    get_filename_component(file_we ${proto_file} NAME_WE)
    
    set(generated_src "${CMAKE_CURRENT_BINARY_DIR}/${file_we}.pb.cc")
    set(generated_hdr "${CMAKE_CURRENT_BINARY_DIR}/${file_we}.pb.h")
    set(generated_grpc_src "${CMAKE_CURRENT_BINARY_DIR}/${file_we}.grpc.pb.cc")
    set(generated_grpc_hdr "${CMAKE_CURRENT_BINARY_DIR}/${file_we}.grpc.pb.h")

    add_custom_command(
        OUTPUT "${generated_src}" "${generated_hdr}" "${generated_grpc_src}" "${generated_grpc_hdr}"
        COMMAND protobuf::protoc
        ARGS --cpp_out "${CMAKE_CURRENT_BINARY_DIR}"
             --grpc_out "${CMAKE_CURRENT_BINARY_DIR}"
             "--plugin=protoc-gen-grpc=$<TARGET_FILE:gRPC::grpc_cpp_plugin>"
             -I "${proto_dir}"
             "${proto_file}"
        DEPENDS "${proto_file}"
        COMMENT "Running C++ gRPC compiler on ${proto_file}"
        VERBATIM
    )

    set(${out_srcs} "${generated_src}" PARENT_SCOPE)
    set(${out_hdrs} "${generated_hdr}" PARENT_SCOPE)
    set(${out_grpc_srcs} "${generated_grpc_src}" PARENT_SCOPE)
    set(${out_grpc_hdrs} "${generated_grpc_hdr}" PARENT_SCOPE)
endfunction()
