.PHONY: test test-docker build

test:
	dotnet test

test-docker:
	./scripts/test-in-docker.sh

build:
	dotnet build
