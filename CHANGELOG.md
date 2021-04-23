# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Support for "pre-wash" custom XSLT transformations that run before validations and standard
  transformations

### Changed

- Force default security tag to closed (as set on SIP Creator command line)

### New settings

- `XSLWEBPREWASHFOLDER` to define the location of the custom XML stylesheets

## 1.0.0 - 2021-04-01

Initial release, supporting validation and transformation from ToPX to Preservica XIPv4.

[Unreleased]: https://github.com/noord-hollandsarchief/preingest/compare/v1.0.0...HEAD
