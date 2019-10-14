cmake_minimum_required(VERSION 3.5)
project(raisimUnity VERSION 0.1.0)

find_package(raisim 0.4.0 CONFIG REQUIRED)
find_package(Eigen3 REQUIRED HINTS ${Eigen3_HINT})

# set default resource directory
set(resource_directory Assets/Resources/objects/)

# server examples
add_subdirectory(examples)