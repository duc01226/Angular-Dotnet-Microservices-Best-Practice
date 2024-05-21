﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PlatformExampleApp.TextSnippet.Domain.ValueObjects;
using PlatformExampleApp.TextSnippet.Persistence.PostgreSql;

#nullable disable

namespace PlatformExampleApp.TextSnippet.Persistence.PostgreSql.Migrations
{
    [DbContext(typeof(TextSnippetDbContext))]
    [Migration("20240501054246_UpdateOutboxIndexes")]
    partial class UpdateOutboxIndexes
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Easy.Platform.Application.MessageBus.InboxPattern.PlatformInboxBusMessage", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(400)
                        .HasColumnType("character varying(400)");

                    b.Property<Guid?>("ConcurrencyUpdateToken")
                        .IsConcurrencyToken()
                        .HasColumnType("uuid");

                    b.Property<string>("ConsumeStatus")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ConsumerBy")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("JsonMessage")
                        .HasColumnType("text");

                    b.Property<DateTime>("LastConsumeDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("LastConsumeError")
                        .HasColumnType("text");

                    b.Property<string>("MessageTypeFullName")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<DateTime?>("NextRetryProcessAfter")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ProduceFrom")
                        .HasColumnType("text");

                    b.Property<int?>("RetriedProcessCount")
                        .HasColumnType("integer");

                    b.Property<string>("RoutingKey")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.HasKey("Id");

                    b.HasIndex("ConsumeStatus", "CreatedDate");

                    b.HasIndex("ConsumeStatus", "LastConsumeDate", "CreatedDate");

                    b.ToTable("PlatformInboxEventBusMessage", (string)null);
                });

            modelBuilder.Entity("Easy.Platform.Application.MessageBus.OutboxPattern.PlatformOutboxBusMessage", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(400)
                        .HasColumnType("character varying(400)");

                    b.Property<Guid?>("ConcurrencyUpdateToken")
                        .IsConcurrencyToken()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("JsonMessage")
                        .HasColumnType("text");

                    b.Property<DateTime>("LastSendDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("LastSendError")
                        .HasColumnType("text");

                    b.Property<string>("MessageTypeFullName")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<DateTime?>("NextRetryProcessAfter")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int?>("RetriedProcessCount")
                        .HasColumnType("integer");

                    b.Property<string>("RoutingKey")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("SendStatus")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("SendStatus", "CreatedDate");

                    b.HasIndex("SendStatus", "LastSendDate", "CreatedDate");

                    b.ToTable("PlatformOutboxEventBusMessage", (string)null);
                });

            modelBuilder.Entity("Easy.Platform.Persistence.DataMigration.PlatformDataMigrationHistory", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<Guid?>("ConcurrencyUpdateToken")
                        .IsConcurrencyToken()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("LastProcessError")
                        .HasColumnType("text");

                    b.Property<DateTime?>("LastProcessingPingTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Status")
                        .HasColumnType("text");

                    b.HasKey("Name");

                    b.HasIndex("ConcurrencyUpdateToken");

                    b.HasIndex("Status");

                    b.ToTable("ApplicationDataMigrationHistoryDbSet");
                });

            modelBuilder.Entity("PlatformExampleApp.TextSnippet.Domain.Entities.TextSnippetEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<ExampleAddressValueObject>("Address")
                        .HasColumnType("jsonb");

                    b.Property<List<string>>("AddressStrings")
                        .HasColumnType("text[]");

                    b.Property<List<ExampleAddressValueObject>>("Addresses")
                        .HasColumnType("jsonb");

                    b.Property<Guid?>("ConcurrencyUpdateToken")
                        .IsConcurrencyToken()
                        .HasColumnType("uuid");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("CreatedByUserId")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("CreatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("FullText")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("character varying(4000)");

                    b.Property<Guid?>("LastUpdatedBy")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("LastUpdatedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("SnippetText")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<TimeOnly>("TimeOnly")
                        .HasColumnType("time without time zone");

                    b.HasKey("Id");

                    b.HasIndex("Address");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("Address"), "GIN");

                    b.HasIndex("AddressStrings");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("AddressStrings"), "GIN");

                    b.HasIndex("Addresses");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("Addresses"), "GIN");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("CreatedDate");

                    b.HasIndex("LastUpdatedBy");

                    b.HasIndex("LastUpdatedDate");

                    b.HasIndex(new[] { "FullText" }, "IX_TextSnippet_FullText_FullTextSearch")
                        .HasAnnotation("Npgsql:TsVectorConfig", "english");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex(new[] { "FullText" }, "IX_TextSnippet_FullText_FullTextSearch"), "GIN");

                    b.HasIndex(new[] { "SnippetText" }, "IX_TextSnippet_SnippetText_FullTextSearch")
                        .HasAnnotation("Npgsql:TsVectorConfig", "english");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex(new[] { "SnippetText" }, "IX_TextSnippet_SnippetText_FullTextSearch"), "GIN");
                    NpgsqlIndexBuilderExtensions.HasOperators(b.HasIndex(new[] { "SnippetText" }, "IX_TextSnippet_SnippetText_FullTextSearch"), new[] { "gin_trgm_ops" });

                    b.ToTable("TextSnippetEntity");
                });
#pragma warning restore 612, 618
        }
    }
}