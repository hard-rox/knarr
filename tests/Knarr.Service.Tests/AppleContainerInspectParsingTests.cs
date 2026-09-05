using System;
using Knarr.Service.AppleContainerCli;

namespace Knarr.Service.Tests;

/// <summary>
/// Verifies that the Apple <c>container inspect</c> and <c>image inspect</c> payloads are shaped
/// into <see cref="ContainerInspection"/> and <see cref="ImageInspection"/> respectively.
/// </summary>
public class AppleContainerInspectParsingTests
{
    private const string ContainerJson = """
    [
      {
        "configuration": {
          "creationDate": "2026-08-03T01:37:16Z",
          "id": "29551832-4ea7-48b9-ba0e-90e7ad97c330",
          "image": {
            "descriptor": {
              "digest": "sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a",
              "mediaType": "application/vnd.oci.image.index.v1+json",
              "size": 10229
            },
            "reference": "docker.io/library/postgres:latest"
          },
          "initProcess": {
            "arguments": ["postgres"],
            "environment": [
              "PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin",
              "POSTGRES_PASSWORD=123"
            ],
            "executable": "docker-entrypoint.sh",
            "workingDirectory": "/"
          },
          "publishedPorts": [],
          "resources": {
            "cpuOverhead": 1,
            "cpus": 4,
            "memoryInBytes": 1073741824
          }
        },
        "id": "29551832-4ea7-48b9-ba0e-90e7ad97c330",
        "status": {
          "networks": [
            {
              "hostname": "29551832-4ea7-48b9-ba0e-90e7ad97c330",
              "ipv4Address": "192.168.64.2/24",
              "ipv4Gateway": "192.168.64.1",
              "macAddress": "f2:4f:88:64:f4:87",
              "network": "default"
            }
          ],
          "startedDate": "2026-08-03T01:37:17Z",
          "state": "running"
        }
      }
    ]
    """;

    private const string ImageJson = """
    [
      {
        "configuration": {
          "creationDate": "2026-07-16T22:06:02Z",
          "descriptor": {
            "digest": "sha256:3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a",
            "mediaType": "application/vnd.oci.image.index.v1+json",
            "size": 10229
          },
          "name": "docker.io/library/postgres:latest"
        },
        "id": "3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a",
        "variants": [
          {
            "config": {
              "architecture": "arm64",
              "config": {
                "Cmd": ["postgres"],
                "Entrypoint": ["docker-entrypoint.sh"],
                "Env": [
                  "PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin",
                  "PGDATA=/var/lib/postgresql/18/docker",
                  "POSTGRES_PASSWORD=secret"
                ],
                "StopSignal": "SIGINT"
              },
              "os": "linux"
            },
            "digest": "sha256:f1dd3522a7fde8bf8e505f42583ecdc00b4967a4b94d32ba1f890d118fe17335",
            "platform": { "architecture": "arm64", "os": "linux" },
            "size": 160971905
          },
          {
            "config": {
              "architecture": "unknown",
              "config": {},
              "os": "unknown"
            },
            "digest": "sha256:8037e666798328714003d407fa4537c8deae9551528dd94c60019f2b17357535",
            "platform": { "architecture": "unknown", "os": "unknown" },
            "size": 6010555
          }
        ]
      }
    ]
    """;

    [Fact]
    public void ParseContainer_ShapesIdentityAndState()
    {
        ContainerInspection i = AppleContainerCliProvider.ParseAppleContainerInspection(ContainerJson);

        Assert.Equal("29551832-4ea7-48b9-ba0e-90e7ad97c330", i.Id);
        Assert.Equal("29551832-4ea", i.ShortId);
        Assert.Equal("docker.io/library/postgres:latest", i.Image);
        Assert.Equal("running", i.Status);
        Assert.True(i.IsRunning);
    }

    [Fact]
    public void ParseContainer_MapsInitProcess()
    {
        ContainerInspection i = AppleContainerCliProvider.ParseAppleContainerInspection(ContainerJson);

        Assert.Equal(["docker-entrypoint.sh"], i.Entrypoint);
        Assert.Equal(["postgres"], i.Command);
        Assert.Equal("/", i.WorkingDirectory);
    }

    [Fact]
    public void ParseContainer_SplitsEnvironmentOnFirstEquals()
    {
        ContainerInspection i = AppleContainerCliProvider.ParseAppleContainerInspection(ContainerJson);

        Assert.Equal(2, i.EnvironmentVariables.Count);
        Assert.Equal("POSTGRES_PASSWORD", i.EnvironmentVariables[1].Key);
        Assert.Equal("123", i.EnvironmentVariables[1].Value);
    }

    [Fact]
    public void ParseContainer_MapsResources()
    {
        ContainerInspection i = AppleContainerCliProvider.ParseAppleContainerInspection(ContainerJson);

        Assert.Equal(1073741824, i.MemoryBytes);
        Assert.Equal(4_000_000_000L, i.NanoCpus);
    }

    [Fact]
    public void ParseContainer_MapsStatusNetwork()
    {
        ContainerInspection i = AppleContainerCliProvider.ParseAppleContainerInspection(ContainerJson);

        Assert.Single(i.Networks);
        Assert.Equal("default", i.Networks[0].Name);
        Assert.Equal("192.168.64.2", i.Networks[0].IPAddress);
        Assert.Equal(24, i.Networks[0].IPPrefixLen);
        Assert.Equal("192.168.64.1", i.Networks[0].Gateway);
        Assert.Equal("f2:4f:88:64:f4:87", i.Networks[0].MacAddress);
    }

    [Fact]
    public void ParseContainer_SetsStartedAt()
    {
        ContainerInspection i = AppleContainerCliProvider.ParseAppleContainerInspection(ContainerJson);

        Assert.Equal(new DateTime(2026, 8, 3), i.StartedAt!.Value.UtcDateTime.Date);
    }

    [Fact]
    public void ParseImage_ShapesIdentityAndMetadata()
    {
        ImageInspection i = AppleContainerCliProvider.ParseAppleImageInspection(ImageJson);

        Assert.Equal("3a82e1f56c8f0f5616a11103ac3d47e632c3938698946a7ad26da0df1334744a", i.Id);
        Assert.Equal("3a82e1f56c8f", i.ShortId);
        Assert.Equal(["docker.io/library/postgres:latest"], i.RepoTags);
        Assert.Equal(new DateTime(2026, 7, 16), i.Created!.Value.UtcDateTime.Date);
    }

    [Fact]
    public void ParseImage_PopulatesRepoDigestFromDescriptor()
    {
        ImageInspection i = AppleContainerCliProvider.ParseAppleImageInspection(ImageJson);

        Assert.Single(i.RepoDigests);
        Assert.Contains("3a82e1f56c8f", i.RepoDigests[0], StringComparison.Ordinal);
    }

    [Fact]
    public void ParseImage_PicksFirstKnownArchitectureVariant()
    {
        ImageInspection i = AppleContainerCliProvider.ParseAppleImageInspection(ImageJson);

        Assert.Equal("arm64", i.Architecture);
        Assert.Equal("linux", i.Os);
    }

    [Fact]
    public void ParseImage_MapsVariantConfig()
    {
        ImageInspection i = AppleContainerCliProvider.ParseAppleImageInspection(ImageJson);

        Assert.Equal(["docker-entrypoint.sh"], i.Entrypoint);
        Assert.Equal(["postgres"], i.Command);
        Assert.Equal("SIGINT", i.StopSignal);
    }

    [Fact]
    public void ParseImage_SplitsEnvironmentOnFirstEquals()
    {
        ImageInspection i = AppleContainerCliProvider.ParseAppleImageInspection(ImageJson);

        Assert.Equal(3, i.EnvironmentVariables.Count);
        InspectionEntry pgdata = i.EnvironmentVariables[1];
        Assert.Equal("PGDATA", pgdata.Key);
        Assert.Equal("/var/lib/postgresql/18/docker", pgdata.Value);
    }

    [Fact]
    public void ParseImage_SumsSizeAcrossAllVariants()
    {
        ImageInspection i = AppleContainerCliProvider.ParseAppleImageInspection(ImageJson);

        Assert.Equal(160971905 + 6010555, i.SizeBytes);
    }

    [Fact]
    public void ParseContainer_PrettyPrintsRawJson()
    {
        ContainerInspection i = AppleContainerCliProvider.ParseAppleContainerInspection(ContainerJson);

        Assert.Contains("postgres", i.RawJson, StringComparison.Ordinal);
        Assert.Contains('\n', i.RawJson);
    }

    [Fact]
    public void ParseImage_PrettyPrintsRawJson()
    {
        ImageInspection i = AppleContainerCliProvider.ParseAppleImageInspection(ImageJson);

        Assert.Contains("postgres", i.RawJson, StringComparison.Ordinal);
        Assert.Contains('\n', i.RawJson);
    }
}
