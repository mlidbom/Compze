# Changelog

All notable changes to Compze.Internals.Serialization.Newtonsoft will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- `NewtonsoftSerializer()`: fills the one serializer parameter an endpoint takes (`EndpointBuilder.Serializer` — registering both wire serializers in one declaration) and the pure client's (`TypermediaClientBuilder.Serializer`).
- The tevent store serializer serializes the whole wrapped tevent - the `ITaggregateIdentifyingTevent<TTeventInterface>` wrapper with its inner tevent inside - as one object graph. The inner tevent's column-backed `TaggregateTevent` properties are still excluded from the json.

## 0.2.0-alpha

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
