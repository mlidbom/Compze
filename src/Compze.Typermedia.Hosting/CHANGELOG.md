# Changelog

All notable changes to Compze.Typermedia.Hosting will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- Removed `ITypermediaTransportServer`: serving is done by the endpoint's one transport server (`IEndpointTransportServer` in `Compze.Internals.Transport`), to which Typermedia contributes its request handling.

## 0.1.0-alpha

- Initial pre-release
