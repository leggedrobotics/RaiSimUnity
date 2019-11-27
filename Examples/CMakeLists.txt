cmake_minimum_required(VERSION 3.5)
project(raisimUnity VERSION 0.1.0)

find_package(raisim 0.4.0 CONFIG REQUIRED)
find_package(Eigen3 REQUIRED HINTS ${Eigen3_HINT})

# set default resource directory
set(resource_directory ${CMAKE_CURRENT_SOURCE_DIR}/rsc/)

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
create_executable(collada src/collada.cpp)
create_executable(anymal src/anymal.cpp)
create_executable(balls src/balls.cpp)
create_executable(atlas src/atlas.cpp)
create_executable(heightmap src/heightmap.cpp)
create_executable(hibernate src/hibernate.cpp)
create_executable(visuals src/visuals.cpp)
create_executable(xmlworld src/xmlworld.cpp)

# EOF
