#==
# Build Targets
#==

function(create_executable app_name file_name)
  add_executable(${app_name} ${file_name})
  target_link_libraries(${app_name} PUBLIC raisim::raisim Eigen3::Eigen pthread)
  target_include_directories(${app_name} PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/include)
  target_compile_definitions(${app_name} PRIVATE "-DEXAMPLE_ROBOT_RESOURCE_DIR=${resource_directory}")
endfunction()

create_executable(primitives src/primitives.cpp)
create_executable(meshes src/meshes.cpp)
create_executable(anymal src/anymal.cpp)
create_executable(heightmap src/heightmap.cpp)

# EOF
