//
// Created by donghok on 09.10.19.

#include "../include/raisim/World.hpp"
#include "../include/raisim/RaisimServer.hpp"
#define TO_STRING(x) _TO_STRING(x)
#define _TO_STRING(x) #x

int main() {
  raisim::World world("/home/donghok/Workspace/git/raisim/res/configFiles/visuals.xml");

  raisim::RaisimServer server(&world);
  server.launchServer();

  raisim::MSLEEP(5000);

  for(int i=0; i<20000; i) {
    raisim::MSLEEP(2);
    server.integrateWorldThreadSafe();
  }

  server.killServer();
}